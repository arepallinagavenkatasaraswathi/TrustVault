
import { CommonModule } from '@angular/common';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Component, ChangeDetectorRef, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';

interface DocumentItem {
  id: string;
  fileName: string;
  createdAt: string;
  isOwner: boolean;
}

interface AuditLogItem {
  id: string;
  documentId: string | null;
  action: string;
  success: boolean;
  timestamp: string;
  details: string;
}

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App implements OnInit {
  private readonly apiUrl = 'http://localhost:5200/api';

  username = '';
  password = '';

  isRegister = false;
  isLoggedIn = false;
  isLoading = false;
  isUploading = false;
  isLoadingAudit = false;

  message = '';
  selectedFile: File | null = null;

  documents: DocumentItem[] = [];
  auditLogs: AuditLogItem[] = [];

  shareUserNames: Record<string, string> = {};
  sharingDocumentId = '';
  revokingDocumentId = '';

  constructor(
    private http: HttpClient,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    const token = localStorage.getItem('trustvault_token');

    if (token && !this.isTokenExpired(token)) {
      this.isLoggedIn = true;
      this.loadDocuments();
      this.loadAuditLogs();
    } else {
      localStorage.removeItem('trustvault_token');
    }
  }

  private isTokenExpired(token: string): boolean {
    try {
      const payload = token.split('.')[1];
      const decoded = JSON.parse(atob(payload));
      return !decoded.exp || decoded.exp * 1000 <= Date.now();
    } catch {
      return true;
    }
  }

  private getHeaders(): HttpHeaders {
    const token = localStorage.getItem('trustvault_token');

    return new HttpHeaders({
      Authorization: `Bearer ${token}`
    });
  }

  switchMode(register: boolean): void {
    this.isRegister = register;
    this.message = '';
    this.password = '';
  }

  submitForm(): void {
    if (!this.username.trim() || !this.password) {
      this.message = 'Please enter your username and password.';
      return;
    }

    if (this.isRegister && this.password.length < 12) {
      this.message = 'Password must contain at least 12 characters.';
      return;
    }

    this.isLoading = true;
    this.message = '';

    const endpoint = this.isRegister ? 'register' : 'login';

    this.http.post<any>(
      `${this.apiUrl}/Auth/${endpoint}`,
      {
        userName: this.username.trim(),
        password: this.password
      }
    ).subscribe({
      next: (response) => {
        this.isLoading = false;

        if (this.isRegister) {
          this.message = 'Registration successful! Please sign in.';
          this.isRegister = false;
          this.password = '';
          this.cdr.detectChanges();
          return;
        }

        if (!response?.token) {
          this.message = 'Login response did not contain a token.';
          this.cdr.detectChanges();
          return;
        }

        localStorage.setItem('trustvault_token', response.token);

        this.isLoggedIn = true;
        this.message = 'Login successful!';
        this.cdr.detectChanges();

        this.loadDocuments();
        this.loadAuditLogs();
      },
      error: (error) => {
        this.isLoading = false;
        this.message =
          typeof error.error === 'string'
            ? error.error
            : error.error?.message ||
              'Login failed. Please check your username and password.';

        this.cdr.detectChanges();
      }
    });
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0] ?? null;

    this.message = '';

    if (!file) {
      this.selectedFile = null;
      return;
    }

    if (file.size > 10 * 1024 * 1024) {
      this.message = 'File size must not exceed 10 MB.';
      input.value = '';
      this.selectedFile = null;
      return;
    }

    this.selectedFile = file;
  }

  uploadDocument(): void {
    if (!this.selectedFile) {
      this.message = 'Please select a document first.';
      return;
    }

    const formData = new FormData();
    formData.append('file', this.selectedFile);

    this.isUploading = true;
    this.message = '';

    this.http.post(
      `${this.apiUrl}/Documents/upload`,
      formData,
      { headers: this.getHeaders() }
    ).subscribe({
      next: () => {
        this.isUploading = false;
        this.selectedFile = null;
        this.message = 'Document uploaded successfully!';

        this.loadDocuments();
        this.loadAuditLogs();
        this.cdr.detectChanges();
      },
      error: (error) => {
        this.isUploading = false;
        this.message =
          error.status === 401
            ? 'Your session expired. Please sign in again.'
            : 'Upload failed. Please try again.';

        this.cdr.detectChanges();
      }
    });
  }

  loadDocuments(): void {
    if (!localStorage.getItem('trustvault_token')) {
      return;
    }

    this.http.get<DocumentItem[]>(
      `${this.apiUrl}/Documents`,
      { headers: this.getHeaders() }
    ).subscribe({
      next: (documents) => {
        this.documents = documents;
        this.cdr.detectChanges();
      },
      error: (error) => {
        if (error.status === 401) {
          this.message = 'Your session expired. Please sign in again.';
          this.cdr.detectChanges();
        }
      }
    });
  }

  shareDocument(doc: DocumentItem): void {
    const targetUser = (this.shareUserNames[doc.id] || '').trim();

    if (!targetUser) {
      this.message = 'Enter the recipient username first.';
      return;
    }

    if (!doc.isOwner) {
      this.message = 'Only the document owner can share this file.';
      return;
    }

    this.sharingDocumentId = doc.id;
    this.message = '';

    this.http.post<any>(
      `${this.apiUrl}/Permissions/${encodeURIComponent(doc.id)}/share`,
      { userName: targetUser },
      { headers: this.getHeaders() }
    ).subscribe({
      next: (response) => {
        this.sharingDocumentId = '';
        this.message =
          response?.message || `Read access granted to ${targetUser}.`;

        this.shareUserNames[doc.id] = '';
        this.loadDocuments();
        this.loadAuditLogs();
        this.cdr.detectChanges();
      },
      error: (error) => {
        this.sharingDocumentId = '';
        this.message =
          typeof error.error === 'string'
            ? error.error
            : error.error?.message ||
              'Sharing failed. Check the username and try again.';

        this.loadAuditLogs();
        this.cdr.detectChanges();
      }
    });
  }

  revokeAccess(doc: DocumentItem): void {
    const targetUser = (this.shareUserNames[doc.id] || '').trim();

    if (!targetUser) {
      this.message = 'Enter the username whose access you want to revoke.';
      return;
    }

    if (!doc.isOwner) {
      this.message = 'Only the document owner can revoke access.';
      return;
    }

    this.revokingDocumentId = doc.id;
    this.message = '';

    this.http.delete<any>(
      `${this.apiUrl}/Permissions/${encodeURIComponent(doc.id)}/share/${encodeURIComponent(targetUser)}`,
      { headers: this.getHeaders() }
    ).subscribe({
      next: (response) => {
        this.revokingDocumentId = '';
        this.message =
          response?.message || `Access revoked for ${targetUser}.`;

        this.shareUserNames[doc.id] = '';
        this.loadDocuments();
        this.loadAuditLogs();
        this.cdr.detectChanges();
      },
      error: (error) => {
        this.revokingDocumentId = '';
        this.message =
          typeof error.error === 'string'
            ? error.error
            : error.error?.message ||
              'Revocation failed. Check the username and active permission.';

        this.loadAuditLogs();
        this.cdr.detectChanges();
      }
    });
  }

  downloadDocument(doc: DocumentItem): void {
    this.message = '';

    this.http.get(
      `${this.apiUrl}/Documents/${encodeURIComponent(doc.id)}/download`,
      {
        headers: this.getHeaders(),
        responseType: 'blob'
      }
    ).subscribe({
      next: (blob) => {
        const url = URL.createObjectURL(blob);
        const link = document.createElement('a');

        link.href = url;
        link.download = doc.fileName;
        link.click();

        URL.revokeObjectURL(url);

        this.message = 'Document downloaded successfully!';
        this.loadAuditLogs();
        this.cdr.detectChanges();
      },
      error: (error) => {
        this.message =
          error.status === 401
            ? 'Your session expired. Please sign in again.'
            : 'Download failed. You may not have permission to access this document.';

        this.loadAuditLogs();
        this.cdr.detectChanges();
      }
    });
  }

  loadAuditLogs(): void {
    if (!localStorage.getItem('trustvault_token')) {
      this.auditLogs = [];
      this.isLoadingAudit = false;
      return;
    }

    this.isLoadingAudit = true;

    this.http.get<AuditLogItem[]>(
      `${this.apiUrl}/AuditLogs`,
      { headers: this.getHeaders() }
    ).subscribe({
      next: (logs) => {
        this.auditLogs = logs;
        this.isLoadingAudit = false;
        this.cdr.detectChanges();
      },
      error: (error) => {
        this.isLoadingAudit = false;

        if (error.status === 401) {
          this.message = 'Your session expired. Please sign in again.';
        } else {
          this.message = 'Unable to load audit history. Please refresh.';
        }

        this.cdr.detectChanges();
      }
    });
  }

  logout(): void {
    localStorage.removeItem('trustvault_token');

    this.isLoggedIn = false;
    this.username = '';
    this.password = '';
    this.selectedFile = null;
    this.documents = [];
    this.auditLogs = [];
    this.shareUserNames = {};
    this.sharingDocumentId = '';
    this.revokingDocumentId = '';
    this.isLoadingAudit = false;
    this.message = '';
    this.isRegister = false;

    this.cdr.detectChanges();
  }
}
