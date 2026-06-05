import { Component, OnInit, inject, signal, computed, PLATFORM_ID } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { CommonModule, isPlatformBrowser } from '@angular/common';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { MetaSeoService } from '../../core/services/meta-seo.service';
import { ApiService } from '../../core/services/api.service';
import { PortfolioPostPin } from '../../core/models';

interface AreaConfig {
  slug: string;
  name: string;
  title: string;
  description: string;
  h1: string;
  lead: string;
  highlight: string;
  faqs: { q: string; a: string }[];
}

const AREAS: Record<string, AreaConfig> = {
  chonburi: {
    slug: 'chonburi',
    name: 'ชลบุรี',
    title: 'ติดตั้งรางน้ำฝน ชลบุรี — ส.จาตุรนต์ รางน้ำ ราคาโปร่งใส',
    description: 'รับติดตั้งรางน้ำฝนสแตนเลส 304 และสังกะสีอาบสี ในเขตชลบุรี ช่างมีประสบการณ์ ประเมินราคาฟรีออนไลน์',
    h1: 'รับติดตั้งรางน้ำฝน ชลบุรี',
    lead: 'บริการติดตั้งรางน้ำฝนครบวงจรในเขตเมืองชลบุรี ช่างมีประสบการณ์มากกว่า 15 ปี ราคาโปร่งใส ไม่มีบิลซ่อน รับประกันผลงาน',
    highlight: 'ชลบุรีมีฝนตกหนักช่วงมรสุม รางน้ำสแตนเลส 304 ทนน้ำ ทนความร้อน ไม่เป็นสนิม เหมาะกับบ้านพักอาศัยและอาคารพาณิชย์ทุกประเภท',
    faqs: [
      { q: 'ราคาติดตั้งรางน้ำในชลบุรีเท่าไหร่?', a: 'ราคาขึ้นอยู่กับวัสดุและความยาว สแตนเลส 304 เริ่มต้น 450 บาท/เมตร สังกะสีอาบสีเริ่มต้น 200 บาท/เมตร ขั้นต่ำ 10 เมตร ประเมินราคาฟรีออนไลน์ได้เลย' },
      { q: 'บริการครอบคลุมพื้นที่ไหนในชลบุรีบ้าง?', a: 'ครอบคลุมทั้งเมืองชลบุรี บ้านบึง บ้านสวน หนองมน แหลมฉบัง และพื้นที่ใกล้เคียง ติดต่อสอบถามได้' },
      { q: 'ต้องรอนานแค่ไหนหลังส่งใบเสนอราคา?', a: 'ทีมงานติดต่อกลับภายใน 1 วันทำการ เพื่อนัดสำรวจหน้างานและยืนยันราคา' },
      { q: 'รับประกันงานติดตั้งไหม?', a: 'รับประกันคุณภาพงานติดตั้ง หากเกิดปัญหาจากการติดตั้ง ทีมงานกลับมาแก้ไขฟรี พร้อม QR Code ใบรับประกัน' }
    ]
  },
  sriracha: {
    slug: 'sriracha',
    name: 'ศรีราชา',
    title: 'ติดตั้งรางน้ำฝน ศรีราชา — สแตนเลสทนชายทะเล ส.จาตุรนต์',
    description: 'รับติดตั้งรางน้ำฝนสแตนเลส 304 ในเขตศรีราชา ทนเกลือชายทะเล ไม่เป็นสนิม อายุการใช้งาน 15+ ปี ประเมินราคาฟรี',
    h1: 'รับติดตั้งรางน้ำฝน ศรีราชา',
    lead: 'บริการติดตั้งรางน้ำฝนในเขตศรีราชาและพื้นที่ใกล้เคียง ช่างผู้เชี่ยวชาญ ราคาโปร่งใส รับประกันผลงาน',
    highlight: 'ศรีราชาอยู่ใกล้ทะเล อากาศเค็มกัดกร่อนโลหะเร็วกว่าพื้นที่ทั่วไป สแตนเลส 304 คือตัวเลือกที่คุ้มค่าที่สุด ทนเกลือ ไม่เป็นสนิม อายุการใช้งาน 15–20 ปี ประหยัดค่าซ่อมบำรุงระยะยาว',
    faqs: [
      { q: 'ทำไมต้องเลือกสแตนเลสสำหรับบ้านในศรีราชา?', a: 'อากาศชายทะเลมีความเค็มสูง กัดกร่อนสังกะสีและเหล็กเร็วกว่าปกติ 2–3 เท่า สแตนเลส 304 ทนเกลือได้ดีกว่า ไม่เป็นสนิม คุ้มค่ากว่าในระยะยาว' },
      { q: 'ราคาสแตนเลสในศรีราชาแพงกว่าทั่วไปไหม?', a: 'ราคาเดียวกันทั้งจังหวัดชลบุรี สแตนเลส 304 เริ่มต้น 450 บาท/เมตร สังกะสีเริ่มต้น 200 บาท/เมตร ขั้นต่ำ 10 เมตร' },
      { q: 'ครอบคลุมพื้นที่ไหนในศรีราชาบ้าง?', a: 'ครอบคลุมศรีราชา บ่อวิน นิคมอมตะ ทุ่งสุขลา และพื้นที่ใกล้เคียง' },
      { q: 'สามารถรื้อรางเก่าและติดตั้งใหม่ได้ไหม?', a: 'รับรื้อถอนรางเก่าพร้อมติดตั้งใหม่ในงานเดียว มีค่าบริการรื้อถอนแยกต่างหาก ดูราคาได้จากเครื่องคำนวณ' }
    ]
  },
  pattaya: {
    slug: 'pattaya',
    name: 'พัทยา',
    title: 'ติดตั้งรางน้ำฝน พัทยา — สแตนเลสทนชายทะเล ส.จาตุรนต์',
    description: 'รับติดตั้งรางน้ำฝนสแตนเลส 304 และสังกะสีอาบสี ในเขตพัทยา ช่างผู้เชี่ยวชาญ บริการรวดเร็ว ประเมินราคาฟรีออนไลน์',
    h1: 'รับติดตั้งรางน้ำฝน พัทยา',
    lead: 'บริการติดตั้งรางน้ำฝนในเขตพัทยาและชลบุรีใต้ ช่างผู้เชี่ยวชาญกว่า 15 ปี รับงานทั้งบ้านพักอาศัย คอนโด รีสอร์ท และอาคารพาณิชย์',
    highlight: 'พัทยาเป็นเมืองชายทะเลที่มีความชื้นสูงตลอดปี สแตนเลส 304 เหมาะกับสภาพอากาศแบบนี้มากที่สุด ไม่เป็นสนิม ดูแลรักษาง่าย ลดต้นทุนระยะยาวได้ชัดเจน',
    faqs: [
      { q: 'รับงานคอนโดและรีสอร์ทในพัทยาไหม?', a: 'รับงานทั้งบ้านพักอาศัย คอนโดมิเนียม รีสอร์ท โรงแรม และอาคารพาณิชย์ ติดต่อสอบถามเพื่อประเมินโปรเจกต์ขนาดใหญ่ได้' },
      { q: 'ราคาติดตั้งรางน้ำในพัทยาเท่าไหร่?', a: 'สแตนเลส 304 เริ่มต้น 450 บาท/เมตร สังกะสีอาบสีเริ่มต้น 200 บาท/เมตร ขั้นต่ำ 10 เมตร มีค่าเดินทาง ใช้เครื่องคำนวณออนไลน์เพื่อดูราคาประมาณการ' },
      { q: 'ครอบคลุมพื้นที่ไหนในพัทยาบ้าง?', a: 'ครอบคลุมพัทยาเหนือ กลาง ใต้ นาเกลือ จอมเทียน และบางเสร่' },
      { q: 'ช่วงฝนตกหนัก ติดตั้งได้ไหม?', a: 'งานติดตั้งต้องทำในสภาพอากาศปกติเพื่อความปลอดภัยและคุณภาพงาน ทีมงานจะนัดหมายวันที่เหมาะสมให้' }
    ]
  }
};

@Component({
  selector: 'app-service-area',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './service-area.component.html',
  styleUrl: './service-area.component.css'
})
export class ServiceAreaComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private metaSeo = inject(MetaSeoService);
  private api = inject(ApiService);
  private sanitizer = inject(DomSanitizer);
  private platformId = inject(PLATFORM_ID);

  area = signal<AreaConfig | null>(null);
  portfolioPins = signal<PortfolioPostPin[]>([]);

  localPins = computed(() => {
    const a = this.area();
    if (!a) return [];
    return this.portfolioPins().filter(p => p.areaName === a.name).slice(0, 6);
  });

  ngOnInit() {
    const slug = this.route.snapshot.paramMap.get('area') ?? '';
    const cfg = AREAS[slug];
    if (!cfg) { this.area.set(null); return; }

    this.area.set(cfg);
    this.metaSeo.set({
      title: cfg.title,
      description: cfg.description,
      canonical: `https://rangnam-chonburi.vercel.app/service/${slug}`
    });

    if (isPlatformBrowser(this.platformId)) {
      this.api.getPortfolioPosts().subscribe({ next: pins => this.portfolioPins.set(pins), error: () => {} });
    }
  }

  safeJsonLd(a: AreaConfig): SafeHtml {
    const json = JSON.stringify({
      '@context': 'https://schema.org',
      '@graph': [
        {
          '@type': 'Service',
          'name': `ติดตั้งรางน้ำฝน ${a.name}`,
          'provider': { '@id': 'https://rangnam-chonburi.vercel.app/#business' },
          'areaServed': { '@type': 'City', 'name': a.name },
          'url': `https://rangnam-chonburi.vercel.app/service/${a.slug}`
        },
        {
          '@type': 'FAQPage',
          'mainEntity': a.faqs.map(f => ({
            '@type': 'Question',
            'name': f.q,
            'acceptedAnswer': { '@type': 'Answer', 'text': f.a }
          }))
        }
      ]
    });
    return this.sanitizer.bypassSecurityTrustHtml(
      `<script type="application/ld+json">${json}<\/script>`
    );
  }
}
