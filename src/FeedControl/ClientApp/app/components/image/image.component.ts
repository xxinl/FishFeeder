import { Component, OnInit } from '@angular/core';
import { Location } from '@angular/common';
import { Router, ActivatedRoute, Params } from '@angular/router';

@Component({
    selector: 'image',
    template: require('./image.component.html')
})

export class ImageComponent implements OnInit {
  public url: string;

  constructor(private route: ActivatedRoute, private location: Location) {
  }

  ngOnInit() {
    this.route.params.forEach((params: Params) => {
      this.url = 'uploads/' + params['url'].replace('%2F', '/');
    });
  }

  goback() {
    this.location.back();
  }
}