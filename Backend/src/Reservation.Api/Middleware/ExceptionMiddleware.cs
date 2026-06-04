using System.Text.Json;
using System.Text.Json.Serialization;
using FluentValidation;
using Reservation.Api.Converters;
using Reservation.Application.Exceptions;
using Reservation.Domain.Exceptions;

namespace Reservation.Api.Middleware;

public sealed class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(), new TimeOnlyHHmmConverter() },
    };

    public async Task InvokeAsync(HttpContext ctx)
    {
        try
        {
            await next(ctx);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception non gérée : {Type}", ex.GetType().Name);
            await WriteErrorAsync(ctx, ex);
        }
    }

    private static async Task WriteErrorAsync(HttpContext ctx, Exception ex)
    {
        var (status, code, details) = Resolve(ex);
        ctx.Response.StatusCode = status;
        ctx.Response.ContentType = "application/json";
        await ctx.Response.WriteAsync(
            JsonSerializer.Serialize(new { code, message = ex.Message, details }, JsonOptions));
    }

    private static (int Status, string Code, object? Details) Resolve(Exception ex) => ex switch
    {
        EntityNotFoundException => (404, "NOT_FOUND", null),

        ValidationException fv => (400, "VALIDATION_ERROR", new
        {
            errors = fv.Errors.Select(e => new { property = e.PropertyName, message = e.ErrorMessage })
        }),

        // ── 400 ──────────────────────────────────────────────────────────────
        SpecialRequestsTooLongException e => (400, "SPECIAL_REQUESTS_TOO_LONG", new
        {
            maxLength = e.MaxLength,
            provided  = e.Provided
        }),

        // ── 403 ──────────────────────────────────────────────────────────────
        InsufficientRoleException e => (403, "INSUFFICIENT_ROLE", new
        {
            required = e.Required.ToString(),
            actual   = e.Actual.ToString()
        }),

        // ── 409 ──────────────────────────────────────────────────────────────
        InvalidStatusTransitionException e => (409, "INVALID_STATUS_TRANSITION", new
        {
            from = e.From.ToString(),
            to   = e.To.ToString()
        }),

        // ── 422 ──────────────────────────────────────────────────────────────
        TimeOutsideServiceException e => (422, "TIME_OUTSIDE_SERVICE", new
        {
            lastBookingTime = e.LastBookingTime.ToString("HH:mm")
        }),

        GuestsBelowMinException e => (422, "GUESTS_BELOW_MIN", new
        {
            minCapacity = e.MinCapacity,
            provided    = e.Provided
        }),

        GuestsExceedCapacityException e => (422, "GUESTS_EXCEED_CAPACITY", new
        {
            capacity = e.Capacity,
            provided = e.Provided
        }),

        ServiceFullyBookedException e => (422, "SERVICE_FULLY_BOOKED", new
        {
            maxCovers    = e.MaxCovers,
            currentTotal = e.CurrentTotal
        }),

        BookingConflictException e => (422, "TABLE_CONFLICT", new
        {
            tableId              = e.TableId,
            conflictingBookingId = e.ConflictingBookingId
        }),

        BookingDateInPastException e => (422, "BOOKING_DATE_IN_PAST", new
        {
            provided = e.Provided.ToString("yyyy-MM-dd")
        }),

        HorizonExceededException e => (422, "HORIZON_EXCEEDED", new
        {
            maxDays  = e.MaxDays,
            provided = e.Provided
        }),

        WalkInMustBeTodayException e => (422, "WALKIN_MUST_BE_TODAY", new
        {
            today    = e.Today.ToString("yyyy-MM-dd"),
            provided = e.Provided.ToString("yyyy-MM-dd")
        }),

        MinLeadTimeViolatedException e => (422, "MIN_LEAD_TIME_VIOLATED", new
        {
            minMinutes  = e.MinMinutes,
            minutesLeft = e.MinutesLeft
        }),

        CustomerBlacklistedException e => (422, "CUSTOMER_BLACKLISTED", new
        {
            customerId = e.CustomerId
        }),

        NoShowTooEarlyException e => (422, "NOSHOW_TOO_EARLY", new
        {
            earliestNoShowAt = e.EarliestNoShowAt
        }),

        CannotCancelSeatedException e => (422, "CANNOT_CANCEL_SEATED", new
        {
            currentStatus = e.CurrentStatus.ToString()
        }),

        RestaurantClosedException e => (422, "RESTAURANT_CLOSED", new
        {
            closedDate = e.ClosedDate.ToString("yyyy-MM-dd"),
            reason     = e.Reason
        }),

        TableNotCombinableException e => (422, "TABLE_NOT_COMBINABLE", new
        {
            tableId = e.TableId
        }),

        DomainException => (422, "DOMAIN_ERROR", null),

        _ => (500, "INTERNAL_SERVER_ERROR", null),
    };
}
