﻿import { provideRouter, RouterConfig } from '@angular/router';

import {UserComponent} from './user.component';
import {UserListComponent} from './user-list/user-list.component';
import {UserAddComponent} from './user-add/user-add.component';

export const userRoutes: RouterConfig = [{
    path: "user",
    component: UserComponent,
    children: [
        { path: '', component: UserListComponent },
        { path: 'add', component: UserAddComponent }
    ]
}];