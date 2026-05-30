import { Component, OnInit, signal, inject } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { CommonModule } from '@angular/common';
import { ApiService } from '../../core/services/api.service';

@Component({
  selector: 'app-thank-you',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="ty-wrap">
      <div class="ty-card">
        <div class="ty-check">✓</div>
        <h1 class="ty-title">ขอบคุณครับ/ค่ะ</h1>
        <p class="ty-sub">รับข้อมูลของท่านเรียบร้อยแล้ว<br>ทีมงานจะติดต่อกลับโดยเร็วที่สุด</p>

        <div class="ty-qn-wrap">
          <div class="ty-qn-label">Quote Number</div>
          <div class="ty-qn">{{ quoteNumber() }}</div>
        </div>

        <div class="ty-actions">
          @if (quoteRequestId()) {
            <a [href]="pdfUrl()" target="_blank" class="btn btn-primary">
              ⬇ ดาวน์โหลดใบเสนอราคา PDF
            </a>
          }
          @if (lineLink()) {
            <a [href]="lineLink()!" target="_blank" class="btn btn-line">
              💬 แชทกับช่างทาง LINE
            </a>
          }
          <a href="/" class="btn btn-secondary">← กลับหน้าหลัก</a>
        </div>
      </div>
    </div>
  `
})
export class ThankYouComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private api = inject(ApiService);

  quoteNumber = signal('');
  quoteRequestId = signal<number | null>(null);
  pdfUrl = signal('');
  lineLink = signal<string | null>(null);

  ngOnInit() {
    const qn = this.route.snapshot.paramMap.get('quoteNumber') ?? '';
    this.quoteNumber.set(qn);
    const nav = history.state;
    if (nav?.quoteRequestId) {
      this.quoteRequestId.set(nav.quoteRequestId);
      this.pdfUrl.set(this.api.getQuotePdfUrl(nav.quoteRequestId));
    }
    this.api.getShopProfile().subscribe(s => {
      if (s.lineOaLink) this.lineLink.set(s.lineOaLink);
    });
  }
}
