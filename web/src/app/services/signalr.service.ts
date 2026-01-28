import { inject, Injectable } from '@angular/core';
import { HubConnection, HubConnectionBuilder, HubConnectionState } from '@microsoft/signalr';
import { AuthService } from './auth.service';
import { Subject } from 'rxjs';

export interface RunUpdatedEvent {
  runId: string;
  status: string;
  updatedAt: string;
}

export interface GroupUpdatedEvent {
  runId: string;
  groupId: string;
  status: string;
}

export interface ScriptUpdatedEvent {
  runId: string;
  groupId: string;
  scriptId: string;
  status: string;
  message?: string;
}

@Injectable({
  providedIn: 'root'
})
export class SignalRService {
  private readonly authService = inject(AuthService);
  private connection: HubConnection | null = null;
  private readonly hubUrl = 'http://localhost:5068/hubs/runs';

  readonly runUpdated$ = new Subject<RunUpdatedEvent>();
  readonly groupUpdated$ = new Subject<GroupUpdatedEvent>();
  readonly scriptUpdated$ = new Subject<ScriptUpdatedEvent>();

  async connect(): Promise<void> {
    if (this.connection?.state === HubConnectionState.Connected) {
      return;
    }

    this.connection = new HubConnectionBuilder()
      .withUrl(this.hubUrl, {
        accessTokenFactory: () => {
          const token = this.authService.getAccessToken();
          if (!token) {
            throw new Error('Not authenticated');
          }
          return token;
        },
        withCredentials: true
      })
      .withAutomaticReconnect()
      .build();

    this.connection.on('run.updated', (event: RunUpdatedEvent) => {
      this.runUpdated$.next(event);
    });

    this.connection.on('group.updated', (event: GroupUpdatedEvent) => {
      this.groupUpdated$.next(event);
    });

    this.connection.on('script.updated', (event: ScriptUpdatedEvent) => {
      this.scriptUpdated$.next(event);
    });

    await this.connection.start();
  }

  async disconnect(): Promise<void> {
    if (this.connection) {
      await this.connection.stop();
      this.connection = null;
    }
  }

  async joinRunGroup(runId: string): Promise<void> {
    if (this.connection?.state === HubConnectionState.Connected) {
      await this.connection.invoke('JoinRunGroup', runId);
    }
  }

  async leaveRunGroup(runId: string): Promise<void> {
    if (this.connection?.state === HubConnectionState.Connected) {
      await this.connection.invoke('LeaveRunGroup', runId);
    }
  }

  async reconnect(): Promise<void> {
    if (this.connection) {
      await this.disconnect();
    }
    await this.connect();
  }
}
