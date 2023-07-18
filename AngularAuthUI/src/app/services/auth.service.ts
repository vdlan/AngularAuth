import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private baseUrl: string = "https://localhost:7127/api/User/";

  constructor(private http: HttpClient) { }

  signUp(userObj: any): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}register`, userObj);
  }

  login(loginObj: any) : Observable<any> {
    return this.http.post<any>(`${this.baseUrl}authenticate`, loginObj);
  }
}
