import { Component, OnInit, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { BaseChartDirective } from 'ng2-charts';
import { ChartConfiguration } from 'chart.js';
import { ApiService } from '../../../core/services/api.service';
import { AuthService } from '../../../core/services/auth.service';
import { Router } from '@angular/router';
import { StatsResponse } from '../../../core/models';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule, BaseChartDirective],
  templateUrl: './dashboard.component.html'
})
export class DashboardComponent implements OnInit {
  private api = inject(ApiService);
  private auth = inject(AuthService);
  private router = inject(Router);

  stats = signal<StatsResponse | null>(null);

  barChartData: ChartConfiguration<'bar'>['data'] = { labels: [], datasets: [] };
  barChartOptions: ChartConfiguration<'bar'>['options'] = {
    responsive: true,
    plugins: { legend: { display: false } },
    scales: {
      x: {
        grid: { color: 'rgba(56,189,248,.06)' },
        ticks: { color: '#607d8b', font: { family: 'IBM Plex Mono', size: 11 } },
        border: { color: '#1a3050' }
      },
      y: {
        beginAtZero: true,
        ticks: { stepSize: 1, color: '#607d8b', font: { family: 'IBM Plex Mono', size: 11 } },
        grid: { color: 'rgba(56,189,248,.06)' },
        border: { color: '#1a3050' }
      }
    }
  };

  ngOnInit() {
    this.api.getStats().subscribe(s => {
      this.stats.set(s);
      this.barChartData = {
        labels: s.weeklySeries.map(w => w.week),
        datasets: [{
          data: s.weeklySeries.map(w => w.count),
          backgroundColor: 'rgba(56,189,248,.25)',
          borderColor: '#38bdf8',
          borderWidth: 1,
          borderRadius: 3,
          hoverBackgroundColor: 'rgba(56,189,248,.4)'
        }]
      };
    });
  }

  logout() {
    this.auth.logout();
    this.router.navigate(['/admin/login']);
  }

  formatNumber(n: number) { return n.toLocaleString('th-TH'); }

  statusLabel(s: string) {
    const map: Record<string, string> = { New: 'ใหม่', Contacted: 'ติดต่อแล้ว', Quoted: 'ส่งใบเสนอราคา', Won: 'ปิดงานได้', Lost: 'ไม่สำเร็จ' };
    return map[s] ?? s;
  }
}
