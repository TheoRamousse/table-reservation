// @unit
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { TableCardComponent } from './table-card.component';
import { TableState, TableStatus, TableZone } from '../../../../core/models/table.model';

const freeTable: TableState = {
  id: 't1', number: 1, capacity: 4, minCapacity: 2,
  zone: TableZone.Salle, isActive: true, isCombinable: false,
  status: TableStatus.Free, activeBooking: null,
};

const confirmedTable: TableState = {
  id: 't2', number: 2, capacity: 4, minCapacity: 2,
  zone: TableZone.Salle, isActive: true, isCombinable: false,
  status: TableStatus.Confirmed,
  activeBooking: { bookingId: 'b1', customerId: 'c1', customerName: 'Dupont Jean', guestsCount: 3, arrivalTime: '12:30', hasAllergyAlert: true, isCelebration: true, needsHighChair: false },
};

describe('TableCardComponent', () => {
  let fixture: ComponentFixture<TableCardComponent>;

  function create(table: TableState) {
    fixture = TestBed.createComponent(TableCardComponent);
    fixture.componentRef.setInput('table', table);
    fixture.detectChanges();
    return fixture.nativeElement as HTMLElement;
  }

  beforeEach(() => TestBed.configureTestingModule({
    imports: [TableCardComponent],
    providers: [provideNoopAnimations()],
  }));

  it('affiche le numéro de table', () => {
    const el = create(freeTable);
    expect(el.textContent).toContain('T1');
  });

  it('affiche le label de statut "Libre"', () => {
    const el = create(freeTable);
    expect(el.textContent).toContain('Libre');
  });

  it('affiche le nom du client quand une réservation est active', () => {
    const el = create(confirmedTable);
    expect(el.textContent).toContain('Dupont Jean');
  });

  it('affiche les flags allergie et célébration', () => {
    const el = create(confirmedTable);
    expect(el.textContent).toContain('⚠️');
    expect(el.textContent).toContain('🎂');
  });

  it('émet tableClick au clic', () => {
    const el = create(freeTable);
    let emitted: TableState | undefined;
    fixture.componentInstance.tableClick.subscribe((t: TableState) => emitted = t);
    el.querySelector<HTMLElement>('div')!.click();
    expect(emitted).toEqual(freeTable);
  });
});
