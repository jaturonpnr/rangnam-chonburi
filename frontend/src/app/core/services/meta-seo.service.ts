import { Injectable, inject } from '@angular/core';
import { Title, Meta } from '@angular/platform-browser';
import { DOCUMENT } from '@angular/common';

export interface PageMeta {
  title: string;
  description: string;
  canonical: string;
  ogTitle?: string;
  ogDescription?: string;
}

@Injectable({ providedIn: 'root' })
export class MetaSeoService {
  private titleSvc = inject(Title);
  private metaSvc = inject(Meta);
  private doc = inject(DOCUMENT);

  set(meta: PageMeta) {
    this.titleSvc.setTitle(meta.title);
    this.metaSvc.updateTag({ name: 'description', content: meta.description });
    this.metaSvc.updateTag({ property: 'og:title', content: meta.ogTitle ?? meta.title });
    this.metaSvc.updateTag({ property: 'og:description', content: meta.ogDescription ?? meta.description });
    this.metaSvc.updateTag({ name: 'twitter:title', content: meta.ogTitle ?? meta.title });
    this.metaSvc.updateTag({ name: 'twitter:description', content: meta.ogDescription ?? meta.description });

    let link = this.doc.querySelector('link[rel="canonical"]') as HTMLLinkElement;
    if (!link) {
      link = this.doc.createElement('link');
      link.setAttribute('rel', 'canonical');
      this.doc.head.appendChild(link);
    }
    link.setAttribute('href', meta.canonical);
  }
}
