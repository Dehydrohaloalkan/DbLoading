import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

export interface DatabaseDto {
  id: string;
  displayName: string;
  server: string;
  database: string;
}

export interface ManagerDto {
  id: string;
  displayName: string;
}

export interface StreamDto {
  id: string;
  displayName: string;
}

export interface StreamsResponseDto {
  managers: ManagerDto[];
  streams: StreamDto[];
}

export interface ScriptDto {
  id: string;
  displayName: string;
  executionLane: number;
  columnsProfileId?: string;
}

export interface ScriptGroupDto {
  id: string;
  displayName: string;
  scripts: ScriptDto[];
}

export interface ColumnItemDto {
  id: string;
  label: string;
}

export interface ColumnProfileDto {
  id: string;
  items: ColumnItemDto[];
}

export interface ScriptsResponseDto {
  groups: ScriptGroupDto[];
  columnsProfiles: ColumnProfileDto[];
}

@Injectable({
  providedIn: 'root'
})
export class CatalogService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = 'http://localhost:5068/api/catalog';

  getDatabases(): Observable<DatabaseDto[]> {
    return this.http.get<DatabaseDto[]>(`${this.apiUrl}/databases`);
  }

  getStreams(): Observable<StreamsResponseDto> {
    return this.http.get<StreamsResponseDto>(`${this.apiUrl}/streams`);
  }

  getScripts(): Observable<ScriptsResponseDto> {
    return this.http.get<ScriptsResponseDto>(`${this.apiUrl}/scripts`);
  }
}
