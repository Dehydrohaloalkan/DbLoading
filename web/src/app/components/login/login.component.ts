import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { Button } from 'primeng/button';
import { Card } from 'primeng/card';
import { InputText } from 'primeng/inputtext';
import { Password } from 'primeng/password';
import { Select } from 'primeng/select';
import { AuthService } from '../../services/auth.service';
import { CatalogService, DatabaseDto, ManagerDto, StreamDto } from '../../services/catalog.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    Card,
    InputText,
    Password,
    Select,
    Button
  ],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss'
})
export class LoginComponent {
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);
  private readonly catalogService = inject(CatalogService);
  private readonly authService = inject(AuthService);

  loginForm: FormGroup;
  databases = signal<DatabaseDto[]>([]);
  managers = signal<ManagerDto[]>([]);
  streams = signal<StreamDto[]>([]);
  isLoading = signal(false);
  errorMessage = signal<string | null>(null);

  constructor() {
    this.loginForm = this.fb.group({
      dbUsername: ['', Validators.required],
      dbPassword: ['', Validators.required],
      databaseId: [null, Validators.required],
      managerId: [null, Validators.required],
      streamId: [null, Validators.required]
    });

    this.loadCatalogData();
  }

  private loadCatalogData(): void {
    this.catalogService.getDatabases().subscribe({
      next: (databases) => this.databases.set(databases),
      error: (err) => {
        console.error('Failed to load databases', err);
        if (err.status === 401) {
          this.authService.clearAuth();
        }
      }
    });

    this.catalogService.getStreams().subscribe({
      next: (streams) => {
        this.managers.set(streams.managers);
        this.streams.set(streams.streams);
      },
      error: (err) => {
        console.error('Failed to load streams', err);
        if (err.status === 401) {
          this.authService.clearAuth();
        }
      }
    });
  }

  onSubmit(): void {
    if (this.loginForm.invalid) {
      return;
    }

    this.isLoading.set(true);
    this.errorMessage.set(null);

    const loginRequest = {
      dbUsername: this.loginForm.value.dbUsername,
      dbPassword: this.loginForm.value.dbPassword,
      databaseId: this.loginForm.value.databaseId,
      managerId: this.loginForm.value.managerId,
      streamId: this.loginForm.value.streamId
    };

    this.authService.login(loginRequest).subscribe({
      next: () => {
        this.isLoading.set(false);
        this.router.navigate(['/main']);
      },
      error: (err) => {
        this.isLoading.set(false);
        this.errorMessage.set(
          err.error?.error || 'Ошибка входа. Проверьте учетные данные.'
        );
      }
    });
  }
}
