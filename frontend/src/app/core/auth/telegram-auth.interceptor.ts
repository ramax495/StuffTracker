import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { TelegramService } from '../../telegram/telegram.service';

/**
 * HTTP interceptor that adds the X-Telegram-Init-Data header to all requests.
 * This header contains the Telegram Mini App initialization data used for
 * authenticating requests on the backend.
 */
export const telegramAuthInterceptor: HttpInterceptorFn = (req, next) => {
  const telegramService = inject(TelegramService);
  const initData = telegramService.getInitData();

  if (initData) {
    const clonedRequest = req.clone({
      setHeaders: {
        'X-Telegram-Init-Data': initData
      }
    });
    return next(clonedRequest);
  }

  return next(req);
};
