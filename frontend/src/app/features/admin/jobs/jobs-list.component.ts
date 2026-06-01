import { Component, OnInit, signal, inject } from '@angular/core';
import { RouterModule } from '@angular/router';
import { CommonModule } from '@angular/common';
import { ApiService } from '../../../core/services/api.service';
import { AuthService } from '../../../core/services/auth.service';
import { Router } from '@angular/router';
import { JobSummary } from '../../../core/models';

@Component({
  selector: 'app-jobs-list',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './jobs-list.component.html'
})
export class JobsListComponent implements OnInit {
  private api = inject(ApiService);
  private auth = inject(AuthService);
  private router = inject(Router);

  jobs = signal<JobSummary[]>([]);
  total = signal(0);
  page = signal(1);
  readonly pageSize = 20;

  ngOnInit() { this.load(); }

  load() {
    this.api.getAdminJobs(this.page(), this.pageSize).subscribe(r => {
      this.jobs.set(r.items);
      this.total.set(r.total);
    });
  }

  prevPage() { if (this.page() > 1) { this.page.update(p => p - 1); this.load(); } }
  nextPage() { if (this.page() * this.pageSize < this.total()) { this.page.update(p => p + 1); this.load(); } }

  materialLabel(m: string) { return m === 'Galvanized' ? 'สังกะสี' : 'สแตนเลส'; }
  logout() { this.auth.logout(); this.router.navigate(['/admin/login']); }
}
