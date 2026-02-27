import { Injectable, signal, computed, inject, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';

/**
 * Toast notification types
 */
export type ToastType = 'error' | 'success' | 'warning' | 'info';

/**
 * Individual toast message configuration
 */
export interface Toast {
  id: number;
  type: ToastType;
  message: string;
  duration: number;
}

/**
 * Toast notification service
 *
 * Manages toast notifications with auto-dismiss functionality.
 * Provides Telegram haptic feedback for error toasts.
 *
 * @example
 * ```typescript
 * this.toastService.error('Failed to save item');
 * this.toastService.success('Item saved successfully');
 * this.toastService.warning('Network connection unstable');
 * this.toastService.info('Syncing data...');
 * ```
 */
@Injectable({
  providedIn: 'root'
})
export class ErrorToastService {
  private readonly platformId = inject(PLATFORM_ID);

  /** Internal counter for generating unique toast IDs */
  private idCounter = 0;

  /** Internal signal storing all active toasts */
  private readonly toastsSignal = signal<Toast[]>([]);

  /** Read-only computed signal of active toasts */
  readonly toasts = computed(() => this.toastsSignal());

  /** Default duration for auto-dismiss in milliseconds */
  private readonly DEFAULT_DURATION = 3000;

  /**
   * Show an error toast notification
   * Triggers haptic feedback on Telegram devices
   *
   * @param message - The error message to display
   * @param duration - Optional auto-dismiss duration in ms (default: 3000)
   */
  error(message: string, duration = this.DEFAULT_DURATION): void {
    this.triggerErrorHapticFeedback();
    this.addToast('error', message, duration);
  }

  /**
   * Show a success toast notification
   *
   * @param message - The success message to display
   * @param duration - Optional auto-dismiss duration in ms (default: 3000)
   */
  success(message: string, duration = this.DEFAULT_DURATION): void {
    this.triggerSuccessHapticFeedback();
    this.addToast('success', message, duration);
  }

  /**
   * Show a warning toast notification
   *
   * @param message - The warning message to display
   * @param duration - Optional auto-dismiss duration in ms (default: 3000)
   */
  warning(message: string, duration = this.DEFAULT_DURATION): void {
    this.triggerWarningHapticFeedback();
    this.addToast('warning', message, duration);
  }

  /**
   * Show an info toast notification
   *
   * @param message - The info message to display
   * @param duration - Optional auto-dismiss duration in ms (default: 3000)
   */
  info(message: string, duration = this.DEFAULT_DURATION): void {
    this.addToast('info', message, duration);
  }

  /**
   * Manually dismiss a toast by its ID
   *
   * @param id - The toast ID to dismiss
   */
  dismiss(id: number): void {
    this.toastsSignal.update(toasts => toasts.filter(t => t.id !== id));
  }

  /**
   * Clear all active toasts
   */
  clearAll(): void {
    this.toastsSignal.set([]);
  }

  /**
   * Add a new toast to the stack
   */
  private addToast(type: ToastType, message: string, duration: number): void {
    const id = ++this.idCounter;

    const toast: Toast = {
      id,
      type,
      message,
      duration
    };

    this.toastsSignal.update(toasts => [...toasts, toast]);

    // Auto-dismiss after duration
    if (duration > 0 && isPlatformBrowser(this.platformId)) {
      setTimeout(() => {
        this.dismiss(id);
      }, duration);
    }
  }

  /**
   * Trigger Telegram haptic feedback for error notifications
   */
  private triggerErrorHapticFeedback(): void {
    if (!isPlatformBrowser(this.platformId)) return;

    try {
      // @ts-expect-error - Telegram WebApp types may not be available
      window.Telegram?.WebApp?.HapticFeedback?.notificationOccurred('error');
    } catch {
      // Silently ignore if haptic feedback is not available
    }
  }

  /**
   * Trigger Telegram haptic feedback for success notifications
   */
  private triggerSuccessHapticFeedback(): void {
    if (!isPlatformBrowser(this.platformId)) return;

    try {
      // @ts-expect-error - Telegram WebApp types may not be available
      window.Telegram?.WebApp?.HapticFeedback?.notificationOccurred('success');
    } catch {
      // Silently ignore if haptic feedback is not available
    }
  }

  /**
   * Trigger Telegram haptic feedback for warning notifications
   */
  private triggerWarningHapticFeedback(): void {
    if (!isPlatformBrowser(this.platformId)) return;

    try {
      // @ts-expect-error - Telegram WebApp types may not be available
      window.Telegram?.WebApp?.HapticFeedback?.notificationOccurred('warning');
    } catch {
      // Silently ignore if haptic feedback is not available
    }
  }
}
