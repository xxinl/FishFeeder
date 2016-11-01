import { Component } from '@angular/core';
import { Http } from '@angular/http';

@Component({
    selector: 'home',
    template: require('./home.component.html')
})
export class HomeComponent {
  
  public status: FeedStatus;

  constructor(private _http: Http) {
    this._http.get('/api/getstatus').subscribe(result => {
      this.status = result.json();
    });
  }

  public feednow() {
    this._http.get('/api/feednow').subscribe(result => {
      });
  }
}

interface FeedStatus {
  feedHour: string;
  lastFeedTime: Date;
  lastPingTime: Date;
  pics: string[];
}
