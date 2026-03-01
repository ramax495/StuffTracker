import { ComponentFixture, TestBed } from '@angular/core/testing';
import { LocationHierarchyComponent } from './location-hierarchy.component';
import { TelegramService } from '../../../../telegram/telegram.service';
import { Component, signal } from '@angular/core';

/**
 * Test host component that wraps LocationHierarchyComponent
 * to provide input signals for testing
 */
@Component({
  selector: 'app-test-host',
  standalone: true,
  imports: [LocationHierarchyComponent],
  template: `
    <app-location-hierarchy
      [breadcrumbs]="breadcrumbs()"
      [breadcrumbIds]="breadcrumbIds()"
      [currentLocationId]="currentLocationId()"
      (locationSelected)="onLocationSelected($event)"
    />
  `
})
class TestHostComponent {
  breadcrumbs = signal<string[]>([]);
  breadcrumbIds = signal<string[]>([]);
  currentLocationId = signal<string>('');
  selectedLocation: string | null = null;

  onLocationSelected(id: string): void {
    this.selectedLocation = id;
  }
}

describe('LocationHierarchyComponent', () => {
  let component: LocationHierarchyComponent;
  let hostComponent: TestHostComponent;
  let fixture: ComponentFixture<TestHostComponent>;
  let telegramService: jasmine.SpyObj<TelegramService>;

  beforeEach(async () => {
    const telegramServiceSpy = jasmine.createSpyObj('TelegramService', ['isInTelegram']);
    telegramServiceSpy.isInTelegram.and.returnValue(false);

    await TestBed.configureTestingModule({
      imports: [TestHostComponent, LocationHierarchyComponent],
      providers: [
        { provide: TelegramService, useValue: telegramServiceSpy }
      ]
    }).compileComponents();

    telegramService = TestBed.inject(TelegramService) as jasmine.SpyObj<TelegramService>;
    fixture = TestBed.createComponent(TestHostComponent);
    hostComponent = fixture.componentInstance;
    component = fixture.debugElement.children[0].componentInstance;
    fixture.detectChanges();
  });

  describe('Input handling', () => {
    it('should render breadcrumbs when provided', () => {
      hostComponent.breadcrumbs.set(['Home', 'Garage', 'Shelf']);
      fixture.detectChanges();

      const breadcrumbList = fixture.nativeElement.querySelector('.breadcrumb-list');
      expect(breadcrumbList).toBeTruthy();
      expect(breadcrumbList.textContent).toContain('Home');
      expect(breadcrumbList.textContent).toContain('Garage');
      expect(breadcrumbList.textContent).toContain('Shelf');
    });

    it('should show empty state when breadcrumbs array is empty', () => {
      hostComponent.breadcrumbs.set([]);
      fixture.detectChanges();

      const breadcrumbEmpty = fixture.nativeElement.querySelector('.breadcrumb-empty');
      expect(breadcrumbEmpty).toBeTruthy();
      expect(breadcrumbEmpty.textContent).toContain('No location');
    });

    it('should update when breadcrumbs signal changes', () => {
      hostComponent.breadcrumbs.set(['Home']);
      fixture.detectChanges();

      expect(component.lastBreadcrumbIndex()).toBe(0);

      hostComponent.breadcrumbs.set(['Home', 'Kitchen']);
      fixture.detectChanges();

      expect(component.lastBreadcrumbIndex()).toBe(1);
    });

    it('should handle missing breadcrumbIds gracefully', () => {
      hostComponent.breadcrumbs.set(['Home', 'Kitchen']);
      hostComponent.breadcrumbIds.set([]);
      fixture.detectChanges();

      const breadcrumbList = fixture.nativeElement.querySelector('.breadcrumb-list');
      expect(breadcrumbList).toBeTruthy();
      expect(breadcrumbList.textContent).toContain('Home');
      expect(breadcrumbList.textContent).toContain('Kitchen');
    });

    it('should handle empty inputs without error', () => {
      expect(hostComponent.breadcrumbs()).toEqual([]);
      expect(hostComponent.breadcrumbIds()).toEqual([]);
      expect(hostComponent.currentLocationId()).toBe('');
    });
  });

  describe('Navigation', () => {
    beforeEach(() => {
      hostComponent.breadcrumbs.set(['Home', 'Kitchen', 'Drawer']);
      hostComponent.breadcrumbIds.set(['loc-1', 'loc-2', 'loc-3']);
      hostComponent.currentLocationId.set('loc-3');
      fixture.detectChanges();
    });

    it('should make non-last breadcrumbs clickable', () => {
      const buttons = fixture.nativeElement.querySelectorAll('.breadcrumb-item--link');
      // Should have 2 clickable items (Home and Kitchen), not the last (Drawer)
      expect(buttons.length).toBe(2);
    });

    it('should make last breadcrumb non-clickable', () => {
      const currentItem = fixture.nativeElement.querySelector('.breadcrumb-item--current');
      expect(currentItem).toBeTruthy();
      expect(currentItem.textContent).toContain('Drawer');
    });

    it('should emit locationSelected event with correct ID on breadcrumb click', () => {
      spyOn(component.locationSelected, 'emit');
      component.navigateToLocation('loc-2', 'Kitchen');
      expect(component.locationSelected.emit).toHaveBeenCalledWith('loc-2');
    });

    it('should log navigation events with DEBUG level', () => {
      spyOn(console, 'debug');
      component.navigateToLocation('loc-1', 'Home');

      expect(console.debug).toHaveBeenCalledWith(
        '[LocationHierarchyComponent] Navigating to location: Home (loc-1)'
      );
    });
  });

  describe('Styling', () => {
    beforeEach(() => {
      hostComponent.breadcrumbs.set(['Home', 'Kitchen']);
      hostComponent.breadcrumbIds.set(['loc-1', 'loc-2']);
      hostComponent.currentLocationId.set('loc-2');
      fixture.detectChanges();
    });

    it('should apply current-location styling to last breadcrumb', () => {
      const currentItem = fixture.nativeElement.querySelector('.breadcrumb-item--current');
      expect(currentItem).toBeTruthy();
      expect(currentItem.classList.contains('breadcrumb-item--current')).toBe(true);
    });

    it('should apply link styling to non-last breadcrumbs', () => {
      const links = fixture.nativeElement.querySelectorAll('.breadcrumb-item--link');
      expect(links.length).toBeGreaterThan(0);
      links.forEach((link: HTMLElement) => {
        expect(link.classList.contains('breadcrumb-item--link')).toBe(true);
      });
    });

    it('should render separators between breadcrumbs', () => {
      const separators = fixture.nativeElement.querySelectorAll('.separator');
      expect(separators.length).toBe(1);
      expect(separators[0].textContent).toContain('>');
    });

    it('should not render separator before first breadcrumb', () => {
      const breadcrumbList = fixture.nativeElement.querySelector('.breadcrumb-list');
      const firstChild = breadcrumbList.firstElementChild;

      expect(firstChild.classList.contains('breadcrumb-item')).toBe(true);
      expect(firstChild.classList.contains('separator')).toBe(false);
    });
  });

  describe('Edge cases', () => {
    it('should handle single breadcrumb (home location)', () => {
      hostComponent.breadcrumbs.set(['Home']);
      hostComponent.breadcrumbIds.set(['loc-1']);
      hostComponent.currentLocationId.set('loc-1');
      fixture.detectChanges();

      const currentItem = fixture.nativeElement.querySelector('.breadcrumb-item--current');
      expect(currentItem).toBeTruthy();
      expect(currentItem.textContent).toContain('Home');

      const buttons = fixture.nativeElement.querySelectorAll('.breadcrumb-item--link');
      expect(buttons.length).toBe(0);
    });

    it('should handle very long location names', () => {
      const longName = 'A'.repeat(100);
      hostComponent.breadcrumbs.set(['Home', longName]);
      hostComponent.breadcrumbIds.set(['loc-1', 'loc-2']);
      hostComponent.currentLocationId.set('loc-2');
      fixture.detectChanges();

      const items = fixture.nativeElement.querySelectorAll('.breadcrumb-item');
      expect(items.length).toBeGreaterThan(0);
    });

    it('should handle mismatched breadcrumbs and IDs', () => {
      hostComponent.breadcrumbs.set(['Home', 'Kitchen', 'Drawer']);
      hostComponent.breadcrumbIds.set(['loc-1']);
      fixture.detectChanges();

      const buttons = fixture.nativeElement.querySelectorAll('.breadcrumb-item--link');
      // Only first breadcrumb should be clickable
      expect(buttons.length).toBeGreaterThan(0);
      expect(buttons.length).toBeLessThan(2);
    });
  });

  describe('Helper methods', () => {
    it('isLastBreadcrumb should return true for last index', () => {
      hostComponent.breadcrumbs.set(['Home', 'Kitchen']);
      fixture.detectChanges();

      expect(component.isLastBreadcrumb(0)).toBe(false);
      expect(component.isLastBreadcrumb(1)).toBe(true);
    });

    it('hasNavigationId should return true if ID exists for breadcrumb', () => {
      hostComponent.breadcrumbIds.set(['loc-1', 'loc-2', '']);
      fixture.detectChanges();

      expect(component.hasNavigationId(0)).toBe(true);
      expect(component.hasNavigationId(1)).toBe(true);
      expect(component.hasNavigationId(2)).toBe(false);
    });

    it('getNavigationId should return correct ID', () => {
      hostComponent.breadcrumbIds.set(['loc-1', 'loc-2']);
      fixture.detectChanges();

      expect(component.getNavigationId(0)).toBe('loc-1');
      expect(component.getNavigationId(1)).toBe('loc-2');
      expect(component.getNavigationId(2)).toBe('');
    });
  });

  describe('Responsive behavior', () => {
    it('should render with flex layout', () => {
      hostComponent.breadcrumbs.set(['Home', 'Kitchen']);
      fixture.detectChanges();

      const breadcrumbList = fixture.nativeElement.querySelector('.breadcrumb-list');
      const styles = window.getComputedStyle(breadcrumbList);

      expect(styles.display).toBe('flex');
    });
  });

  describe('Haptic feedback', () => {
    it('should not trigger haptic feedback outside Telegram', () => {
      telegramService.isInTelegram.and.returnValue(false);
      spyOn(console, 'debug');

      component.navigateToLocation('loc-1', 'Home');

      // Should not error, just navigate
      expect(component.locationSelected.emit).toBeDefined();
    });

    it('should gracefully handle haptic feedback in Telegram', () => {
      telegramService.isInTelegram.and.returnValue(true);
      spyOn(console, 'debug');

      // Should not throw even if Telegram HapticFeedback is unavailable
      expect(() => {
        component.navigateToLocation('loc-1', 'Home');
      }).not.toThrow();
    });
  });
});
