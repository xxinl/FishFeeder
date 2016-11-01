import { Component } from '@angular/core';
import { Http } from '@angular/http';

@Component({
    selector: 'feedhist',
    template: require('./feedhist.component.html')
})
export class FeedHistComponent {
    public feedLogs: FeedLog[];

    constructor(http: Http) {
      http.get('/api/getfeedlogs').subscribe(result => {
        this.feedLogs = result.json();
        });
    }
}

interface FeedLog {
  entryTime: Date;
  pics: string[];
}
