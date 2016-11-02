import { Component, OnInit } from '@angular/core';
import { Http } from '@angular/http';
import { Router, ActivatedRoute, Params } from '@angular/router';

@Component({
    selector: 'feedhist',
    template: require('./feedhist.component.html')
})

export class FeedHistComponent implements OnInit {
  public feedLogs: FeedLog[];
  public isLogScreen: boolean;

  constructor(private http: Http, private route: ActivatedRoute) {
  }

  ngOnInit() {
    this.route.params.forEach((params: Params) => {
      this.isLogScreen = params['islog'] === "true";
    });

    let url = this.isLogScreen ? '/api/geterrors' : '/api/getfeedlogs';

    this.http.get(url).subscribe(result => {
      this.feedLogs = result.json();
    });
  }
}

interface FeedLog {
  entryTime: Date;
  content: string;
  pics: string[];
}
