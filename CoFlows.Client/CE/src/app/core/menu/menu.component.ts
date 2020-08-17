import { Component } from '@angular/core';
//import { MenuService } from './menu.service';
import { CoFlowsComponent } from '../../coflows/core/coflows.component';

import { TranslateService } from '@ngx-translate/core';
/* tslint:disable:max-line-length */
@Component({
  selector: 'app-menu',
  template: `
  <ul class="navigation" appAccordion>
    <li class="navigation-item" appAccordionLink *ngFor="let menuitem of menu" group="{{menuitem.state}}">
      <a class="navigation-link" appAccordionToggle [routerLink]="menuitem.parameters == undefined ? ['/', menuitem.state] : ['/', menuitem.state].concat(menuitem.parameters)" *ngIf="menuitem.type === 'link'">
        <i class="icon icon-{{ menuitem.icon }}"></i>
        <span>{{ menuitem.name | translate }}</span>
        <span class="mr-auto"></span>
        <span class="badge badge-{{ badge.type }}" *ngFor="let badge of menuitem.badge">{{ badge.value }}</span>
      </a>
      <a class="navigation-link" appAccordionToggle [routerLink]="menuitem.parameters == undefined ? ['/', menuitem.state, menuitem.substate] : ['/', menuitem.state, menuitem.substate].concat(menuitem.parameters)" *ngIf="menuitem.type === 'sublink'">
        <i class="icon icon-{{ menuitem.icon }}"></i>
        <span>{{ menuitem.name | translate }}</span>
        <span class="mr-auto"></span>
        <span class="badge badge-{{ badge.type }}" *ngFor="let badge of menuitem.badge">{{ badge.value }}</span>
      </a>
      <a class="navigation-link" appAccordionToggle href="{{menuitem.state}}" *ngIf="menuitem.type === 'extLink'">
        <i class="icon icon-{{ menuitem.icon }}"></i>
        <span>{{ menuitem.name | translate }}</span>
        <span class="mr-auto"></span>
        <span class="badge badge-{{ badge.type }}" *ngFor="let badge of menuitem.badge">{{ badge.value }}</span>
      </a>
      <a class="navigation-link" appAccordionToggle href="{{menuitem.state}}" target="_blank" *ngIf="menuitem.type === 'extTabLink'">
        <i class="icon icon-{{ menuitem.icon }}"></i>
        <span>{{ menuitem.name | translate }}</span>
        <span class="mr-auto"></span>
        <span class="badge badge-{{ badge.type }}" *ngFor="let badge of menuitem.badge">{{ badge.value }}</span>
      </a>
      <a class="navigation-link" appAccordionToggle href="javascript:;" *ngIf="menuitem.type === 'sub'">
        <i class="icon icon-{{ menuitem.icon }}"></i>
        <span>{{ menuitem.name | translate }}</span>
        <span class="mr-auto"></span>
        <span class="badge badge-{{ badge.type }}" *ngFor="let badge of menuitem.badge">{{ badge.value }}</span>
        <i class="menu-caret icon icon-arrows-right"></i>
      </a>
      <ul class="navigation-submenu" *ngIf="menuitem.type === 'sub'">
        <li class="navigation-item" *ngFor="let childitem of menuitem.children" routerLinkActive="open">
          <a [routerLink]="childitem == undefined || childitem.parameters == undefined ? ['/', menuitem.state, childitem.state] : ['/', menuitem.state, childitem.state].concat(childitem.parameters)" class="navigation-link relative">{{ childitem.name | translate }}</a>
        </li>
      </ul>
    </li>
  </ul>`,
  //providers: [MenuService]
})
export class MenuComponent {
  currentLang = 'en';

  menu_function(workflows){

      return [
        {
          state: '/',
          name: 'HOME',
          type: 'link',
          icon: 'basic-accelerator'
        },
        {
          state: 'workflows',
          name: 'Workflows',
          type: 'sub',
          icon: 'software-layers2',
          children: workflows
        },
        {
          state: 'workflows',
          substate: 'topic',
          name: 'Topics',
          parameters: ["-"],
          type: 'sublink',
          icon: 'basic-mixer2'
        }
      ]
  }

  menu = []

  constructor(

    private coflows: CoFlowsComponent,
    public translate: TranslateService) {
        this.coflows.Get('flow/servicedworkflows', data => {
            let wp = []
            data.forEach(element => {
                wp.push({
                    state: 'workflow',
                    parameters: [element.ID],
                    name: element.Name
                });
            });

            this.menu = this.menu_function(wp)
        })
    }
}
