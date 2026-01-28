import { CommonModule } from '@angular/common';
import { Component, inject, OnDestroy, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { Button } from 'primeng/button';
import { Checkbox } from 'primeng/checkbox';
import { MultiSelect } from 'primeng/multiselect';
import { ProgressSpinner } from 'primeng/progressspinner';
import { TabsModule } from 'primeng/tabs';
import { Subscription } from 'rxjs';
import { AuthService } from '../../services/auth.service';
import { CatalogService, ColumnProfileDto, ScriptDto, ScriptGroupDto } from '../../services/catalog.service';
import { RunsService, RunStatus } from '../../services/runs.service';
import { SignalRService } from '../../services/signalr.service';

interface SelectedScript {
  scriptId: string;
  enabled: boolean;
  exportMode: 'Default' | 'CustomColumns';
  selectedColumnItemIds: string[];
}

type StatusType = 'Queued' | 'Running' | 'Success' | 'NoData' | 'Failed' | 'Cancelled';

@Component({
  selector: 'app-main',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    TabsModule,
    Checkbox,
    MultiSelect,
    Button,
    ProgressSpinner
  ],
  templateUrl: './main.component.html',
  styleUrl: './main.component.scss'
})
export class MainComponent implements OnInit, OnDestroy {
  private readonly catalogService = inject(CatalogService);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly runsService = inject(RunsService);
  private readonly signalRService = inject(SignalRService);
  private subscriptions = new Subscription();

  groups = signal<ScriptGroupDto[]>([]);
  columnsProfiles = signal<ColumnProfileDto[]>([]);
  selectedScript = signal<ScriptDto | null>(null);
  selectedProfile = signal<ColumnProfileDto | null>(null);
  selectedColumnItems = signal<string[]>([]);

  scriptSelections = signal<Map<string, SelectedScript>>(new Map());
  activeTab: string | number | undefined = undefined;

  runStatuses = signal<Map<string, RunStatus>>(new Map());
  groupStatuses = signal<Map<string, Map<string, StatusType>>>(new Map());
  scriptStatuses = signal<Map<string, Map<string, Map<string, StatusType>>>>(new Map());

  readonly currentUser = this.authService.currentUser$;

  async ngOnInit(): Promise<void> {
    if (!this.authService.isAuthenticated()) {
      this.router.navigate(['/login']);
      return;
    }

    this.loadScripts();
    await this.connectSignalR();
    this.subscribeToEvents();
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
    this.signalRService.disconnect();
  }

  private async connectSignalR(): Promise<void> {
    if (!this.authService.isAuthenticated()) {
      this.router.navigate(['/login']);
      return;
    }

    try {
      await this.signalRService.connect();
    } catch (error) {
      console.error('Failed to connect to SignalR', error);
      if (error instanceof Error && error.message.includes('Not authenticated')) {
        this.router.navigate(['/login']);
      }
    }
  }

  private subscribeToEvents(): void {
    this.subscriptions.add(
      this.signalRService.runUpdated$.subscribe(event => {
        this.updateRunStatus(event.runId, event.status);
      })
    );

    this.subscriptions.add(
      this.signalRService.groupUpdated$.subscribe(event => {
        this.updateGroupStatus(event.runId, event.groupId, event.status as StatusType);
      })
    );

    this.subscriptions.add(
      this.signalRService.scriptUpdated$.subscribe(event => {
        this.updateScriptStatus(event.runId, event.groupId, event.scriptId, event.status as StatusType);
      })
    );
  }

  private updateRunStatus(runId: string, status: string): void {
    const current = this.runStatuses();
    const run = current.get(runId);
    if (run) {
      const updated = new Map(current);
      updated.set(runId, { ...run, status, updatedAt: new Date().toISOString() });
      this.runStatuses.set(updated);
    }
  }

  private updateGroupStatus(runId: string, groupId: string, status: StatusType): void {
    const current = this.groupStatuses();
    const updated = new Map(current);
    let runGroups = updated.get(runId);
    if (!runGroups) {
      runGroups = new Map();
      updated.set(runId, runGroups);
    }
    runGroups.set(groupId, status);
    this.groupStatuses.set(updated);
  }

  private updateScriptStatus(runId: string, groupId: string, scriptId: string, status: StatusType): void {
    const current = this.scriptStatuses();
    const updated = new Map(current);
    let runScripts = updated.get(runId);
    if (!runScripts) {
      runScripts = new Map();
      updated.set(runId, runScripts);
    }
    let groupScripts = runScripts.get(groupId);
    if (!groupScripts) {
      groupScripts = new Map();
      runScripts.set(groupId, groupScripts);
    }
    groupScripts.set(scriptId, status);
    this.scriptStatuses.set(updated);
  }

  getScriptStatus(runId: string, groupId: string, scriptId: string): StatusType | null {
    return this.scriptStatuses().get(runId)?.get(groupId)?.get(scriptId) || null;
  }

  getGroupStatus(runId: string, groupId: string): StatusType | null {
    return this.groupStatuses().get(runId)?.get(groupId) || null;
  }

  getRunStatus(runId: string): StatusType | null {
    return this.runStatuses().get(runId)?.status as StatusType || null;
  }

  getLastRunId(): string | null {
    const runs = Array.from(this.runStatuses().keys());
    if (runs.length === 0) return null;
    return runs[runs.length - 1];
  }

  getScriptStatusForLastRun(groupId: string, scriptId: string): StatusType | null {
    const lastRunId = this.getLastRunId();
    if (!lastRunId) return null;
    return this.getScriptStatus(lastRunId, groupId, scriptId);
  }

  getGroupStatusForLastRun(groupId: string): StatusType | null {
    const lastRunId = this.getLastRunId();
    if (!lastRunId) return null;
    return this.getGroupStatus(lastRunId, groupId);
  }

  getStatusIcon(status: StatusType | null): string {
    if (!status) return '';
    switch (status) {
      case 'Queued': return 'pi-clock';
      case 'Running': return 'pi-spin pi-spinner';
      case 'Success': return 'pi-check';
      case 'NoData': return 'pi-info-circle';
      case 'Failed': return 'pi-times';
      case 'Cancelled': return 'pi-ban';
      default: return '';
    }
  }

  getStatusClass(status: StatusType | null): string {
    if (!status) return '';
    switch (status) {
      case 'Queued': return 'status-queued';
      case 'Running': return 'status-running';
      case 'Success': return 'status-success';
      case 'NoData': return 'status-nodata';
      case 'Failed': return 'status-failed';
      case 'Cancelled': return 'status-cancelled';
      default: return '';
    }
  }

  private loadScripts(): void {
    this.catalogService.getScripts().subscribe({
      next: (response) => {
        this.groups.set(response.groups);
        this.columnsProfiles.set(response.columnsProfiles);
        if (response.groups.length > 0) {
          this.activeTab = response.groups[0].id;
        }
      },
      error: (err) => {
        console.error('Failed to load scripts', err);
        if (err.status === 401) {
          this.authService.clearAuth();
          this.router.navigate(['/login']);
        }
      }
    });
  }

  onScriptSelect(script: ScriptDto): void {
    this.selectedScript.set(script);
    
    if (script.columnsProfileId) {
      const profile = this.columnsProfiles().find(p => p.id === script.columnsProfileId);
      this.selectedProfile.set(profile || null);
      
      const existing = this.scriptSelections().get(script.id);
      if (existing) {
        this.selectedColumnItems.set(existing.selectedColumnItemIds);
      } else {
        this.selectedColumnItems.set([]);
      }
    } else {
      this.selectedProfile.set(null);
      this.selectedColumnItems.set([]);
    }
  }

  onScriptToggle(script: ScriptDto, enabled: boolean): void {
    const current = this.scriptSelections();
    const updated = new Map(current);
    
    if (enabled) {
      updated.set(script.id, {
        scriptId: script.id,
        enabled: true,
        exportMode: 'Default',
        selectedColumnItemIds: []
      });
    } else {
      updated.delete(script.id);
    }
    
    this.scriptSelections.set(updated);
  }

  onExportModeChange(mode: 'Default' | 'CustomColumns'): void {
    const script = this.selectedScript();
    if (!script) return;

    const current = this.scriptSelections();
    const updated = new Map(current);
    const existing = updated.get(script.id) || {
      scriptId: script.id,
      enabled: true,
      exportMode: 'Default' as const,
      selectedColumnItemIds: []
    };

    updated.set(script.id, {
      ...existing,
      exportMode: mode
    });

    this.scriptSelections.set(updated);
  }

  onColumnItemsChange(items: string[]): void {
    const script = this.selectedScript();
    if (!script) return;

    this.selectedColumnItems.set(items);

    const current = this.scriptSelections();
    const updated = new Map(current);
    const existing = updated.get(script.id) || {
      scriptId: script.id,
      enabled: true,
      exportMode: 'CustomColumns' as const,
      selectedColumnItemIds: []
    };

    updated.set(script.id, {
      ...existing,
      selectedColumnItemIds: items
    });

    this.scriptSelections.set(updated);
  }

  isScriptEnabled(scriptId: string): boolean {
    return this.scriptSelections().get(scriptId)?.enabled || false;
  }

  getScriptExportMode(scriptId: string): 'Default' | 'CustomColumns' {
    return this.scriptSelections().get(scriptId)?.exportMode || 'Default';
  }

  runGroup(groupId: string): void {
    const group = this.groups().find(g => g.id === groupId);
    if (!group) return;

    const enabledScripts = group.scripts.filter(s => this.isScriptEnabled(s.id));
    if (enabledScripts.length === 0) return;

    const selection = enabledScripts.map(script => {
      const selection = this.scriptSelections().get(script.id);
      return {
        scriptId: script.id,
        enabled: true,
        exportMode: selection?.exportMode || 'Default' as const,
        selectedColumnItemIds: selection?.selectedColumnItemIds || []
      };
    });

    const request = {
      mode: 'SelectedGroups' as const,
      selection: {
        groups: [{
          groupId: groupId,
          enabled: true,
          scripts: selection
        }]
      }
    };

    this.runsService.startRun(request).subscribe({
      next: async (response) => {
        await this.signalRService.joinRunGroup(response.runId);
        this.runsService.getRun(response.runId).subscribe({
          next: (status) => {
            const current = this.runStatuses();
            const updated = new Map(current);
            updated.set(response.runId, status);
            this.runStatuses.set(updated);

            const currentGroupStatuses = this.groupStatuses();
            const currentScriptStatuses = this.scriptStatuses();
            const updatedGroupStatuses = new Map(currentGroupStatuses);
            const updatedScriptStatuses = new Map(currentScriptStatuses);

            if (!updatedGroupStatuses.has(response.runId)) {
              updatedGroupStatuses.set(response.runId, new Map());
            }
            const runGroupStatuses = updatedGroupStatuses.get(response.runId)!;

            Object.entries(status.groupStatuses).forEach(([gId, gStatus]) => {
              runGroupStatuses.set(gId, gStatus as StatusType);
            });

            if (!updatedScriptStatuses.has(response.runId)) {
              updatedScriptStatuses.set(response.runId, new Map());
            }
            const runScriptStatuses = updatedScriptStatuses.get(response.runId)!;

            Object.entries(status.scriptStatuses).forEach(([gId, scripts]) => {
              if (!runScriptStatuses.has(gId)) {
                runScriptStatuses.set(gId, new Map());
              }
              const groupMap = runScriptStatuses.get(gId)!;
              Object.entries(scripts).forEach(([sId, sStatus]) => {
                groupMap.set(sId, sStatus as StatusType);
              });
            });

            this.groupStatuses.set(updatedGroupStatuses);
            this.scriptStatuses.set(updatedScriptStatuses);
          },
          error: (err) => console.error('Failed to get run status', err)
        });
      },
      error: (err) => console.error('Failed to start run', err)
    });
  }

  runAllGroups(): void {
    const enabledGroups = this.groups().filter(group => 
      group.scripts.some(s => this.isScriptEnabled(s.id))
    );

    if (enabledGroups.length === 0) return;

    const groups = enabledGroups.map(group => {
      const enabledScripts = group.scripts.filter(s => this.isScriptEnabled(s.id));
      return {
        groupId: group.id,
        enabled: true,
        scripts: enabledScripts.map(script => {
          const selection = this.scriptSelections().get(script.id);
          return {
            scriptId: script.id,
            enabled: true,
            exportMode: selection?.exportMode || 'Default' as const,
            selectedColumnItemIds: selection?.selectedColumnItemIds || []
          };
        })
      };
    });

    const request = {
      mode: 'AllGroups' as const,
      selection: { groups }
    };

    this.runsService.startRun(request).subscribe({
      next: async (response) => {
        await this.signalRService.joinRunGroup(response.runId);
        this.runsService.getRun(response.runId).subscribe({
          next: (status) => {
            const current = this.runStatuses();
            const updated = new Map(current);
            updated.set(response.runId, status);
            this.runStatuses.set(updated);

            const currentGroupStatuses = this.groupStatuses();
            const currentScriptStatuses = this.scriptStatuses();
            const updatedGroupStatuses = new Map(currentGroupStatuses);
            const updatedScriptStatuses = new Map(currentScriptStatuses);

            if (!updatedGroupStatuses.has(response.runId)) {
              updatedGroupStatuses.set(response.runId, new Map());
            }
            const runGroupStatuses = updatedGroupStatuses.get(response.runId)!;

            Object.entries(status.groupStatuses).forEach(([gId, gStatus]) => {
              runGroupStatuses.set(gId, gStatus as StatusType);
            });

            if (!updatedScriptStatuses.has(response.runId)) {
              updatedScriptStatuses.set(response.runId, new Map());
            }
            const runScriptStatuses = updatedScriptStatuses.get(response.runId)!;

            Object.entries(status.scriptStatuses).forEach(([gId, scripts]) => {
              if (!runScriptStatuses.has(gId)) {
                runScriptStatuses.set(gId, new Map());
              }
              const groupMap = runScriptStatuses.get(gId)!;
              Object.entries(scripts).forEach(([sId, sStatus]) => {
                groupMap.set(sId, sStatus as StatusType);
              });
            });

            this.groupStatuses.set(updatedGroupStatuses);
            this.scriptStatuses.set(updatedScriptStatuses);
          },
          error: (err) => console.error('Failed to get run status', err)
        });
      },
      error: (err) => console.error('Failed to start run', err)
    });
  }

  getEnabledScriptsCount(groupId: string): number {
    const group = this.groups().find(g => g.id === groupId);
    if (!group) return 0;
    return group.scripts.filter(s => this.isScriptEnabled(s.id)).length;
  }

  logout(): void {
    this.authService.logout().subscribe({
      next: () => {
        this.router.navigate(['/login']);
      },
      error: () => {
        this.router.navigate(['/login']);
      }
    });
  }
}
