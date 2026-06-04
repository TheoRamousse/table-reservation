import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
} from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatTooltipModule } from '@angular/material/tooltip';
import { AuthService } from '../../core/services/auth.service';

interface NavItem {
  label: string;
  route: string;
  icon: string;
}

@Component({
  selector: 'app-nav',
  templateUrl: './nav.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    RouterLink,
    RouterLinkActive,
    MatButtonModule,
    MatIconModule,
    MatMenuModule,
    MatTooltipModule,
  ],
})
export class NavComponent {
  private readonly authService = inject(AuthService);

  protected readonly role = this.authService.role;
  protected readonly isAuthenticated = this.authService.isAuthenticated;

  protected readonly staffItems = computed<NavItem[]>(() => [
    { label: 'Vue salle', route: '/floor', icon: 'table_restaurant' },
    { label: 'Disponibilités', route: '/availability', icon: 'search' },
  ]);

  protected readonly managerItems = computed<NavItem[]>(() => {
    const r = this.role();
    if (r !== 'Manager' && r !== 'Admin') return [];
    return [
      { label: 'Jours de fermeture', route: '/admin/closed-days', icon: 'event_busy' },
      { label: 'Clients', route: '/admin/customers', icon: 'people' },
    ];
  });

  protected readonly adminItems = computed<NavItem[]>(() => {
    if (this.role() !== 'Admin') return [];
    return [
      { label: 'Tables', route: '/admin/tables', icon: 'chair' },
    ];
  });

  protected readonly hasAdminSection = computed(
    () => this.managerItems().length > 0 || this.adminItems().length > 0,
  );

  protected logout(): void {
    this.authService.logout();
  }
}
