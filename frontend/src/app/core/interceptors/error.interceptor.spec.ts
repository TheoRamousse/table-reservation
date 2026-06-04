// @integration
import { TestBed } from '@angular/core/testing';
import {
  HttpClient,
  HttpErrorResponse,
  provideHttpClient,
  withInterceptors,
} from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { MatSnackBar } from '@angular/material/snack-bar';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { errorInterceptor } from './error.interceptor';

describe('errorInterceptor', () => {
  let httpClient: HttpClient;
  let httpMock: HttpTestingController;
  let snackBarSpy: jasmine.SpyObj<MatSnackBar>;

  beforeEach(() => {
    snackBarSpy = jasmine.createSpyObj<MatSnackBar>('MatSnackBar', ['open']);

    TestBed.configureTestingModule({
      providers: [
        provideNoopAnimations(),
        provideHttpClient(withInterceptors([errorInterceptor])),
        provideHttpClientTesting(),
        { provide: MatSnackBar, useValue: snackBarSpy },
      ],
    });

    httpClient = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('affiche le message métier pour une erreur 422', () => {
    httpClient.get('/api/test').subscribe({ error: () => {} });

    const req = httpMock.expectOne('/api/test');
    req.flush(
      { code: 'TABLE_CONFLICT', message: 'Créneau déjà occupé sur cette table.' },
      { status: 422, statusText: 'Unprocessable Entity' }
    );

    expect(snackBarSpy.open).toHaveBeenCalledWith(
      'Créneau déjà occupé sur cette table.',
      'Fermer',
      jasmine.objectContaining({ duration: 5000 })
    );
  });

  it('affiche "Ressource introuvable" pour une erreur 404', () => {
    httpClient.get('/api/test').subscribe({ error: () => {} });

    const req = httpMock.expectOne('/api/test');
    req.flush({}, { status: 404, statusText: 'Not Found' });

    expect(snackBarSpy.open).toHaveBeenCalledWith(
      'Ressource introuvable.',
      'Fermer',
      jasmine.objectContaining({ duration: 5000 })
    );
  });

  it('affiche "Erreur serveur, réessayez" pour une erreur 500', () => {
    httpClient.get('/api/test').subscribe({ error: () => {} });

    const req = httpMock.expectOne('/api/test');
    req.flush({}, { status: 500, statusText: 'Internal Server Error' });

    expect(snackBarSpy.open).toHaveBeenCalledWith(
      'Erreur serveur, réessayez.',
      'Fermer',
      jasmine.objectContaining({ duration: 5000 })
    );
  });

  it('affiche un message générique pour une erreur réseau (status 0)', () => {
    httpClient.get('/api/test').subscribe({ error: () => {} });

    const req = httpMock.expectOne('/api/test');
    req.error(new ProgressEvent('error'));

    expect(snackBarSpy.open).toHaveBeenCalledWith(
      'Erreur réseau, vérifiez votre connexion.',
      'Fermer',
      jasmine.objectContaining({ duration: 5000 })
    );
  });
});
