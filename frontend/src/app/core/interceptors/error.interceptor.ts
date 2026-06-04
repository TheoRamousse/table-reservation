import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { catchError, throwError } from 'rxjs';
import { ApiError } from '../models/api-error.model';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const snackBar = inject(MatSnackBar);

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      let message: string;

      if (error.status === 0) {
        message = 'Erreur réseau, vérifiez votre connexion.';
      } else if (error.error && (error.error as ApiError).message) {
        message = (error.error as ApiError).message;
      } else if (error.status === 404) {
        message = 'Ressource introuvable.';
      } else if (error.status >= 500) {
        message = 'Erreur serveur, réessayez.';
      } else {
        message = 'Une erreur est survenue.';
      }

      snackBar.open(message, 'Fermer', { duration: 5000 });
      return throwError(() => error.error ?? error);
    })
  );
};
