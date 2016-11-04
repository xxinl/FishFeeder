import { Component, OnInit } from '@angular/core';
import { Http } from '@angular/http';

@Component({
  selector: 'cam',
  template: require('./cam.component.html')
})

export class CamComponent {
  
  constructor(private http: Http) {
  }
}


import { Directive } from '@angular/core';
import { DomSanitizer } from '@angular/platform-browser';

@Directive({
  selector: '[cam-image]',
  host: {
    '[src]': 'sanitizer.bypassSecurityTrustUrl(imageData)'
  }
})

export class CamImageDirective implements OnInit {
  imageData: any;

  constructor(private http: Http,
    private sanitizer: DomSanitizer) { }

  ngOnInit() {
    setInterval(() => {
      this.http.get("/api/streamdown")
        .subscribe(
        data => {
          this.imageData = 'data:image/jpg;base64,' + data.json();
        }
        );
    }, 1000);
  }
}