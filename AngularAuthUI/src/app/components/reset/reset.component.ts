import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { NgToastService } from 'ng-angular-popup';
import { ConfirmPasswordValidator } from 'src/app/helpers/confirm-password.validator';
import ValidateForm from 'src/app/helpers/validateform';
import { ResetPassword } from 'src/app/models/reset-password.model';
import { ResetPasswordService } from 'src/app/services/reset-password.service';

@Component({
  selector: 'app-reset',
  templateUrl: './reset.component.html',
  styleUrls: ['./reset.component.scss']
})
export class ResetComponent implements OnInit {
  resetPasswordForm!: FormGroup;
  emailToReset!: string;
  emailToken!: string;
  resetPasswordObject = new ResetPassword();

  constructor(private fb: FormBuilder,
     private activatedRoute: ActivatedRoute,
     private resetPasswordService: ResetPasswordService,
     private toast: NgToastService,
     private router: Router) { }

  ngOnInit(): void {
    this.resetPasswordForm = this.fb.group({
      password: [null, Validators.required],
      confirmPassword: [null, Validators.required],
    }, {
      validator: ConfirmPasswordValidator('password', 'confirmPassword')
    })

    this.activatedRoute.queryParams.subscribe(val => {
      this.emailToReset = val['email'];
      let uriToken = val['code'];
      this.emailToken = uriToken.replace(/ /g, '+');
    })
  }

  reset() {
    if(this.resetPasswordForm.valid) {
      this.resetPasswordObject.email = this.emailToReset;
      this.resetPasswordObject.newPassword = this.resetPasswordForm.value.password;
      this.resetPasswordObject.confirmPassword = this.resetPasswordForm.value.confirmPassword;
      this.resetPasswordObject.emailToken = this.emailToken;

      this.resetPasswordService.resetPassword(this.resetPasswordObject).subscribe({
        next: (res) => {
          this.toast.success({detail: "SUCCESS", summary: 'Password reset successfully!', duration: 3000});
          this.router.navigate(['/']);
        },
        error: (err) => {
          this.toast.success({detail: "ERROR", summary: 'Something went wrong!', duration: 3000});
        }
      });
    }
    else {
      ValidateForm.validateAllForm(this.resetPasswordForm);
    }
  }

}
