import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

export interface StartRunRequest {
  mode: 'AllGroups' | 'SelectedGroups';
  selection: RunSelection;
}

export interface RunSelection {
  groups: GroupSelection[];
}

export interface GroupSelection {
  groupId: string;
  enabled: boolean;
  scripts: ScriptSelection[];
}

export interface ScriptSelection {
  scriptId: string;
  enabled: boolean;
  exportMode: 'Default' | 'CustomColumns';
  selectedColumnItemIds?: string[];
}

export interface StartRunResponse {
  runId: string;
  status: string;
}

export interface RunStatus {
  runId: string;
  status: string;
  createdAt: string;
  updatedAt?: string;
  groupStatuses: Record<string, string>;
  scriptStatuses: Record<string, Record<string, string>>;
}

@Injectable({
  providedIn: 'root'
})
export class RunsService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = 'http://localhost:5068/api/runs';

  startRun(request: StartRunRequest): Observable<StartRunResponse> {
    return this.http.post<StartRunResponse>(`${this.apiUrl}`, request);
  }

  getRun(runId: string): Observable<RunStatus> {
    return this.http.get<RunStatus>(`${this.apiUrl}/${runId}`);
  }

  cancelRun(runId: string): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.apiUrl}/${runId}/cancel`, {});
  }
}
