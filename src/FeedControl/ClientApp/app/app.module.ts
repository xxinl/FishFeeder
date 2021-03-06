import { NgModule } from '@angular/core';
import { RouterModule } from '@angular/router';
import { UniversalModule } from 'angular2-universal';
import { AppComponent } from './components/app/app.component'
import { NavMenuComponent } from './components/navmenu/navmenu.component';
import { HomeComponent } from './components/home/home.component';
import { FeedHistComponent } from './components/feedhist/feedhist.component';
import { ImageComponent } from './components/image/image.component';
import { CamComponent, CamImageDirective } from "./components/cam/cam.component";

@NgModule({
  bootstrap: [AppComponent],
  declarations: [
    AppComponent,
    NavMenuComponent,
    FeedHistComponent,
    HomeComponent,
    ImageComponent,
    CamComponent,
    CamImageDirective
  ],
  imports: [
    UniversalModule, // Must be first import. This automatically imports BrowserModule, HttpModule, and JsonpModule too.
    RouterModule.forRoot([
      { path: '', redirectTo: 'home', pathMatch: 'full' },
      { path: 'home', component: HomeComponent },
      { path: 'feed-hist/:islog', component: FeedHistComponent },
      { path: 'image/:url', component: ImageComponent },
      { path: 'cam', component: CamComponent },
      { path: '**', redirectTo: 'home' }
    ])
  ]
})
export class AppModule {
}
