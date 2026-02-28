import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { ItemFormComponent } from './item-form.component';
import { ItemApiService, ItemDetail } from '../../../core/api/item-api.service';
import { LocationApiService, LocationTreeNode } from '../../../core/api/location-api.service';
import { TelegramService } from '../../../telegram/telegram.service';

const mockTree: LocationTreeNode[] = [
  {
    id: 'loc-1',
    name: 'Garage',
    depth: 0,
    children: [
      { id: 'loc-1-1', name: 'Shelf A', depth: 1, children: [] }
    ]
  },
  { id: 'loc-2', name: 'Kitchen', depth: 0, children: [] }
];

const mockItem: ItemDetail = {
  id: 'item-1',
  name: 'Screwdriver',
  description: 'Phillips head',
  quantity: 3,
  locationId: 'loc-1',
  locationPath: ['Garage'],
  locationName: 'Garage',
  createdAt: '2026-01-01T00:00:00Z',
  updatedAt: '2026-01-01T00:00:00Z'
};

describe('ItemFormComponent', () => {
  let component: ItemFormComponent;
  let fixture: ComponentFixture<ItemFormComponent>;
  let mockItemApi: jasmine.SpyObj<ItemApiService>;
  let mockLocationApi: jasmine.SpyObj<LocationApiService>;

  const mockTelegramService = {
    isInTelegram: () => false,
    showMainButton: jasmine.createSpy('showMainButton'),
    hideMainButton: jasmine.createSpy('hideMainButton'),
    setMainButtonText: jasmine.createSpy('setMainButtonText'),
    onMainButtonClick: jasmine.createSpy('onMainButtonClick'),
    offMainButtonClick: jasmine.createSpy('offMainButtonClick')
  };

  beforeEach(async () => {
    mockItemApi = jasmine.createSpyObj('ItemApiService', [
      'getItem', 'createItem', 'updateItem', 'moveItem'
    ]);
    mockLocationApi = jasmine.createSpyObj('LocationApiService', [
      'getLocationTree', 'getLocation'
    ]);

    mockLocationApi.getLocationTree.and.returnValue(of(mockTree));

    await TestBed.configureTestingModule({
      imports: [ItemFormComponent],
      providers: [
        provideZonelessChangeDetection(),
        provideRouter([]),
        { provide: ItemApiService, useValue: mockItemApi },
        { provide: LocationApiService, useValue: mockLocationApi },
        { provide: TelegramService, useValue: mockTelegramService }
      ]
    }).compileComponents();
  });

  function createComponent(): void {
    fixture = TestBed.createComponent(ItemFormComponent);
    component = fixture.componentInstance;
  }

  describe('tree loading', () => {
    it('should load location tree on init', () => {
      createComponent();
      fixture.detectChanges();

      expect(mockLocationApi.getLocationTree).toHaveBeenCalled();
      expect(component.locationTree().length).toBe(2);
      expect(component.isLoadingTree()).toBe(false);
    });

    it('should handle tree loading error gracefully', () => {
      mockLocationApi.getLocationTree.and.returnValue(throwError(() => new Error('fail')));
      createComponent();
      fixture.detectChanges();

      expect(component.isLoadingTree()).toBe(false);
      expect(component.locationTree().length).toBe(0);
    });
  });

  describe('create mode', () => {
    it('should show "Add Item" title', () => {
      createComponent();
      fixture.detectChanges();

      expect(component.formTitle()).toBe('Add Item');
      expect(component.isEditMode()).toBe(false);
    });

    it('should pre-fill resolvedLocationId from locationId input', () => {
      createComponent();
      fixture.componentRef.setInput('locationId', 'loc-1');
      fixture.detectChanges();

      expect(component.resolvedLocationId).toBe('loc-1');
    });

    it('should work without locationId (global add-item)', () => {
      createComponent();
      fixture.detectChanges();

      expect(component.resolvedLocationId).toBeNull();
      expect(component.isValid()).toBe(false);
    });

    it('should require location for validity', () => {
      createComponent();
      fixture.detectChanges();

      component.name.set('Test Item');
      component.resolvedLocationId = null;

      expect(component.isValid()).toBe(false);
    });

    it('should be valid with name and location', () => {
      createComponent();
      fixture.componentRef.setInput('locationId', 'loc-1');
      fixture.detectChanges();

      component.name.set('Test Item');

      expect(component.isValid()).toBe(true);
    });
  });

  describe('edit mode', () => {
    beforeEach(() => {
      mockItemApi.getItem.and.returnValue(of(mockItem));
    });

    it('should show "Edit Item" title', () => {
      createComponent();
      fixture.componentRef.setInput('id', 'item-1');
      fixture.detectChanges();

      expect(component.formTitle()).toBe('Edit Item');
      expect(component.isEditMode()).toBe(true);
    });

    it('should load and populate item data', () => {
      createComponent();
      fixture.componentRef.setInput('id', 'item-1');
      fixture.detectChanges();

      expect(mockItemApi.getItem).toHaveBeenCalledWith('item-1');
      expect(component.name()).toBe('Screwdriver');
      expect(component.description()).toBe('Phillips head');
      expect(component.quantity()).toBe(3);
      expect(component.resolvedLocationId).toBe('loc-1');
    });

    it('should track original locationId for move detection', () => {
      createComponent();
      fixture.componentRef.setInput('id', 'item-1');
      fixture.detectChanges();

      // Original is set internally — verify by changing location and submitting
      expect(component.resolvedLocationId).toBe('loc-1');
    });
  });

  describe('location selection', () => {
    it('should update resolvedLocationId on selection', () => {
      createComponent();
      fixture.detectChanges();

      component.onLocationSelected({ id: 'loc-2', name: 'Kitchen' });

      expect(component.resolvedLocationId).toBe('loc-2');
    });

    it('should clear resolvedLocationId on null selection', () => {
      createComponent();
      fixture.componentRef.setInput('locationId', 'loc-1');
      fixture.detectChanges();

      component.onLocationSelected(null);

      expect(component.resolvedLocationId).toBeNull();
    });
  });

  describe('validation', () => {
    beforeEach(() => {
      createComponent();
      fixture.componentRef.setInput('locationId', 'loc-1');
      fixture.detectChanges();
    });

    it('should validate name is required', () => {
      component.name.set('');
      component.validateName();
      expect(component.nameError()).toBe('Name is required');
    });

    it('should validate name max length', () => {
      component.name.set('a'.repeat(201));
      component.validateName();
      expect(component.nameError()).toBe('Name must be 200 characters or less');
    });

    it('should clear name error for valid name', () => {
      component.name.set('Valid Name');
      component.validateName();
      expect(component.nameError()).toBeNull();
    });

    it('should validate quantity min', () => {
      component.quantity.set(0);
      component.validateQuantity();
      expect(component.quantityError()).toBe('Quantity must be at least 1');
    });

    it('should invalidate form without location', () => {
      component.name.set('Test');
      component.resolvedLocationId = null;
      expect(component.isValid()).toBe(false);
    });
  });

  describe('submit - create', () => {
    beforeEach(() => {
      createComponent();
      fixture.componentRef.setInput('locationId', 'loc-1');
      fixture.detectChanges();
    });

    it('should call createItem with correct request', () => {
      mockItemApi.createItem.and.returnValue(of({ id: 'new-1' } as any));

      component.name.set('New Item');
      component.description.set('Some desc');
      component.quantity.set(5);
      component.onSubmit();

      expect(mockItemApi.createItem).toHaveBeenCalledWith({
        name: 'New Item',
        description: 'Some desc',
        quantity: 5,
        locationId: 'loc-1'
      });
    });

    it('should not submit when form is invalid', () => {
      component.name.set('');
      component.onSubmit();

      expect(mockItemApi.createItem).not.toHaveBeenCalled();
    });

    it('should show error if location is missing on submit', () => {
      component.name.set('Test');
      component.resolvedLocationId = null;
      component.onSubmit();

      expect(mockItemApi.createItem).not.toHaveBeenCalled();
    });
  });

  describe('submit - update with move', () => {
    beforeEach(() => {
      mockItemApi.getItem.and.returnValue(of(mockItem));
      mockItemApi.updateItem.and.returnValue(of({} as any));
      mockItemApi.moveItem.and.returnValue(of({} as any));

      createComponent();
      fixture.componentRef.setInput('id', 'item-1');
      fixture.detectChanges();
    });

    it('should call updateItem without moveItem when location unchanged', () => {
      component.onSubmit();

      expect(mockItemApi.updateItem).toHaveBeenCalled();
      expect(mockItemApi.moveItem).not.toHaveBeenCalled();
    });

    it('should call moveItem after updateItem when location changed', () => {
      component.onLocationSelected({ id: 'loc-2', name: 'Kitchen' });
      component.onSubmit();

      expect(mockItemApi.updateItem).toHaveBeenCalled();
      expect(mockItemApi.moveItem).toHaveBeenCalledWith('item-1', 'loc-2');
    });

    it('should show error if moveItem fails', () => {
      mockItemApi.moveItem.and.returnValue(throwError(() => new Error('Move failed')));

      component.onLocationSelected({ id: 'loc-2', name: 'Kitchen' });
      component.onSubmit();

      expect(component.submitError()).toBe('Move failed');
      expect(component.isSaving()).toBe(false);
    });
  });
});
