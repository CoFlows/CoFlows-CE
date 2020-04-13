import {
  Component, OnInit, ElementRef, OnDestroy,
  ViewChild, HostListener, NgZone, AfterViewInit } from '@angular/core';
import { Title } from '@angular/platform-browser';
import { Router, ActivatedRoute, NavigationEnd } from '@angular/router';
import { NgbModal, ModalDismissReasons } from '@ng-bootstrap/ng-bootstrap';

import { Subscription } from 'rxjs';
import { filter } from 'rxjs/operators';

import { TranslateService } from '@ngx-translate/core';

import { CoFlowsComponent } from '../../coflows/core/coflows.component';

const SMALL_WIDTH_BREAKPOINT = 991;

export interface Options {
  heading?: string;
  removeFooter?: boolean;
  mapHeader?: boolean;
}

@Component({
  selector: 'app-layout',
  templateUrl: './admin-layout.component.html',
  styleUrls: ['./admin-layout.component.scss']
})
export class AdminLayoutComponent implements OnInit, OnDestroy, AfterViewInit {

  private _router: Subscription;
  private mediaMatcher: MediaQueryList = matchMedia(`(max-width: ${SMALL_WIDTH_BREAKPOINT}px)`);

  routeOptions: Options;

  options = {
    lang: 'en',
    theme: 'light',
    settings: false,
    docked: false,
    boxed: false,
    opened: true,
    // mode: 'push'
    mode: 'dock' //Arturo,
  };

  _mode = this.options.mode;
  _autoCollapseWidth = 991;

  

  currentLang = 'en';

  @ViewChild('sidebar') sidebar;

  constructor (
    public coflows: CoFlowsComponent,
    private _element: ElementRef,
    private router: Router,
    private route: ActivatedRoute,
    public translate: TranslateService,
    private modalService: NgbModal,
    private titleService: Title,
    private zone: NgZone) {
      const browserLang: string = translate.getBrowserLang();
      translate.use(browserLang.match(/en|fr/) ? browserLang : 'en');
      // this.mediaMatcher.addListener(mql => zone.run(() => this.mediaMatcher = mql));
  }

  ngOnInit(): void {
    this._router = this.router.events.pipe(filter(event => event instanceof NavigationEnd)).subscribe((event: NavigationEnd) => {
      // Scroll to top on view load
      document.querySelector('.main-content').scrollTop = 0;

      if (this.isOver()) {
        this._mode = 'over';
        this.options.opened = false;
      }

      this.runOnRouteChange();
    });

    this.runOnRouteChange();
  }

  ngAfterViewInit(): void  {
    setTimeout(_ => this.runOnRouteChange());
  }

  ngOnDestroy() {
    this._router.unsubscribe();
  }

  runOnRouteChange(): void {
    
    CoFlowsComponent.ClearInstruments()
    
    if (this.isOver() || this.router.url === '/maps/fullscreen') {
      this.options.opened = false;
    }

    this.route.children.forEach((route: ActivatedRoute) => {
      let activeRoute: ActivatedRoute = route;
      while (activeRoute.firstChild) {
        activeRoute = activeRoute.firstChild;
      }
      this.routeOptions = activeRoute.snapshot.data;
    });

    if (this.routeOptions) {
      if (this.routeOptions.hasOwnProperty('heading')) {
        this.setTitle(this.routeOptions.heading);
      }
    }
  }

  setTitle( newTitle: string) {
    //this.titleService.setTitle( 'Decima - Bootstrap 4 Angular Admin Template | ' + newTitle );
    this.titleService.setTitle( 'CoFlows - Collaborative WorkFlows | ' + newTitle ); //Arturo
  }

  isOver(): boolean {
    return this.mediaMatcher.matches;
  }

  toogleSidebar(): void {
    this.options.opened = !this.options.opened;
  }

  receiveMessage($event) {
    this.options = $event;
  }

  openSearch(search) {
    this.modalService.open(search, { windowClass: 'search', backdrop: false });
  }

  toggleFullscreen(): void {
    const elem = this._element.nativeElement.querySelector('.main-content');
    if (elem.requestFullscreen) {
      elem.requestFullscreen();
    } else if (elem.webkitRequestFullScreen) {
      elem.webkitRequestFullScreen();
    } else if (elem.mozRequestFullScreen) {
      elem.mozRequestFullScreen();
    } else if (elem.msRequestFullScreen) {
      elem.msRequestFullScreen();
    }
  }
}
