import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ItemDetailComponent } from './item-detail.component';
import { LocationHierarchyComponent } from '../components/location-hierarchy/location-hierarchy.component';
import { ItemApiService, ItemDetail } from '../../../core/api/item-api.service';
import { LocationApiService } from '../../../core/api/location-api.service';
import { TelegramService } from '../../../telegram/telegram.service';
import { Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { Component, signal } from '@angular/core';

/**
 * Test host component to set up ItemDetailComponent with proper input
 */
@Component({
  selector: 'app-test-item-detail-host',
  standalone: true,
  imports: [ItemDetailComponent, LocationHierarchyComponent],
  template: `<app-item-detail [id]="itemId()" />`
})
class TestHostComponent {
  itemId = signal<string>('item-1');
}

describe('ItemDetailComponent - Location Hierarchy Integration', () => {
  let hostComponent: TestHostComponent;
  let itemDetailComponent: ItemDetailComponent;
  let fixture: ComponentFixture<TestHostComponent>;
  let itemApiService: jasmine.SpyObj<ItemApiService>;
  let locationApiService: jasmine.SpyObj<LocationApiService>;
  let telegramService: jasmine.SpyObj<TelegramService>;
  let router: jasmine.SpyObj<Router>;

  const mockItemDetail: ItemDetail = {
    id: 'item-1',
    name: 'Test Item',
    quantity: 5,
    description: 'Test description',
    locationId: 'loc-3',
    locationName: 'Drawer',
    locationPath: ['Home', 'Kitchen'],
    createdAt: '2026-03-01T10:00:00Z',
    updatedAt: '2026-03-01T10:00:00Z'
  };

  const mockLocationResponse = {
    id: 'loc-3',
    name: 'Drawer',
    breadcrumbs: ['Home', 'Kitchen', 'Drawer'],
    breadcrumbIds: ['loc-1', 'loc-2', 'loc-3'],
    depth: 3,
    createdAt: '2026-02-28T10:00:00Z',
    updatedAt: '2026-02-28T10:00:00Z',
    children: [],
    items: []
  };

  beforeEach(async () => {
    const itemApiServiceSpy = jasmine.createSpyObj('ItemApiService', [
      'getItem',
      'deleteItem'
    ]);
    const locationApiServiceSpy = jasmine.createSpyObj('LocationApiService', [
      'getLocation'
    ]);
    const telegramServiceSpy = jasmine.createSpyObj('TelegramService', [
      'isInTelegram',
      'hideMainButton'
    ]);
    const routerSpy = jasmine.createSpyObj('Router', ['navigate']);

    await TestBed.configureTestingModule({
      imports: [TestHostComponent, ItemDetailComponent, LocationHierarchyComponent],
      providers: [
        { provide: ItemApiService, useValue: itemApiServiceSpy },
        { provide: LocationApiService, useValue: locationApiServiceSpy },
        { provide: TelegramService, useValue: telegramServiceSpy },
        { provide: Router, useValue: routerSpy }
      ]
    }).compileComponents();

    itemApiService = TestBed.inject(ItemApiService) as jasmine.SpyObj<ItemApiService>;
    locationApiService = TestBed.inject(LocationApiService) as jasmine.SpyObj<LocationApiService>;
    telegramService = TestBed.inject(TelegramService) as jasmine.SpyObj<TelegramService>;
    router = TestBed.inject(Router) as jasmine.SpyObj<Router>;

    telegramService.hideMainButton.and.stub();
    telegramService.isInTelegram.and.returnValue(false);

    itemApiService.getItem.and.returnValue(of(mockItemDetail));
    locationApiService.getLocation.and.returnValue(of(mockLocationResponse));

    fixture = TestBed.createComponent(TestHostComponent);
    hostComponent = fixture.componentInstance;
    itemDetailComponent = fixture.debugElement.children[0].componentInstance;
  });

  describe('Component initialization', () => {
    it('should create', () => {
      expect(itemDetailComponent).toBeTruthy();
    });

    it('should load item on init', () => {
      fixture.detectChanges();
      expect(itemApiService.getItem).toHaveBeenCalledWith('item-1');
    });
  });

  describe('Location hierarchy integration', () => {
    beforeEach(() => {
      fixture.detectChanges();
    });

    it('should render LocationHierarchyComponent when item loads', () => {
      const hierarchyComponent = fixture.nativeElement.querySelector('app-location-hierarchy');
      expect(hierarchyComponent).toBeTruthy();
    });

    it('should pass correct breadcrumbs to LocationHierarchyComponent', () => {
      const breadcrumbs = itemDetailComponent.getBreadcrumbs();
      expect(breadcrumbs).toContain('Home');
      expect(breadcrumbs).toContain('Kitchen');
      expect(breadcrumbs).toContain('Test Item');
    });

    it('should update breadcrumbIds after location loads', (done) => {
      setTimeout(() => {
        expect(itemDetailComponent.breadcrumbIds()).toEqual(['loc-1', 'loc-2', 'loc-3']);
        done();
      }, 100);
    });

    it('should navigate when breadcrumb is selected', () => {
      itemDetailComponent.navigateToLocationHierarchy('loc-2');
      expect(router.navigate).toHaveBeenCalledWith(['/location', 'loc-2']);
    });

    it('should log breadcrumb navigation', () => {
      spyOn(console, 'debug');
      itemDetailComponent.navigateToLocationHierarchy('loc-2');

      expect(console.debug).toHaveBeenCalledWith(
        '[ItemDetail] Navigating from breadcrumb to location: loc-2'
      );
    });
  });

  describe('Data loading', () => {
    it('should set loading state while fetching', () => {
      expect(itemDetailComponent.isLoading()).toBe(true);
      fixture.detectChanges();
      expect(itemDetailComponent.isLoading()).toBe(false);
    });

    it('should handle location loading failure gracefully', (done) => {
      locationApiService.getLocation.and.returnValue(
        throwError(() => new Error('Location not found'))
      );
      spyOn(console, 'warn');

      fixture.detectChanges();

      setTimeout(() => {
        expect(console.warn).toHaveBeenCalled();
        // Breadcrumbs should still be displayed even if location loading fails
        expect(itemDetailComponent.getBreadcrumbs().length).toBeGreaterThan(0);
        done();
      }, 100);
    });
  });

  describe('Signal lifecycle', () => {
    beforeEach(() => {
      fixture.detectChanges();
    });

    it('should maintain breadcrumb IDs in signal', (done) => {
      setTimeout(() => {
        expect(itemDetailComponent.breadcrumbIds()).toEqual(mockLocationResponse.breadcrumbIds);
        done();
      }, 100);
    });

    it('should preserve current location ID', () => {
      expect(itemDetailComponent.item()?.locationId).toBe('loc-3');
    });
  });

  describe('Error handling', () => {
    it('should display error when item load fails', (done) => {
      itemApiService.getItem.and.returnValue(
        throwError(() => new Error('Failed to load item'))
      );

      fixture.detectChanges();

      setTimeout(() => {
        expect(itemDetailComponent.error()).toContain('Failed to load');
        done();
      }, 100);
    });
  });

  describe('Breadcrumb display', () => {
    it('should include full path in breadcrumbs', () => {
      fixture.detectChanges();

      const breadcrumbs = itemDetailComponent.getBreadcrumbs();
      expect(breadcrumbs).toEqual(['Home', 'Kitchen', 'Test Item']);
    });
  });
});
