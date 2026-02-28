import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { LocationAutocompleteComponent } from './location-autocomplete.component';
import { TelegramService } from '../../../telegram/telegram.service';
import { LocationTreeNode } from '../../../core/api/location-api.service';

const mockTree: LocationTreeNode[] = [
  {
    id: 'loc-1',
    name: 'Garage',
    depth: 0,
    children: [
      {
        id: 'loc-1-1',
        name: 'Shelf A',
        depth: 1,
        children: [
          { id: 'loc-1-1-1', name: 'Box 1', depth: 2, children: [] }
        ]
      }
    ]
  },
  {
    id: 'loc-2',
    name: 'Kitchen',
    depth: 0,
    children: []
  }
];

describe('LocationAutocompleteComponent', () => {
  let component: LocationAutocompleteComponent;
  let fixture: ComponentFixture<LocationAutocompleteComponent>;

  const mockTelegramService = {
    isInTelegram: () => false
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [LocationAutocompleteComponent],
      providers: [
        provideZonelessChangeDetection(),
        { provide: TelegramService, useValue: mockTelegramService }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(LocationAutocompleteComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    fixture.detectChanges();
    expect(component).toBeTruthy();
  });

  describe('flatten tree', () => {
    it('should flatten a nested tree into a flat list', () => {
      fixture.componentRef.setInput('locations', mockTree);
      fixture.detectChanges();

      const flat = component.flatLocations();
      expect(flat.length).toBe(4);
      expect(flat.map(l => l.name)).toEqual(['Garage', 'Shelf A', 'Box 1', 'Kitchen']);
    });

    it('should compute path hints for nested locations', () => {
      fixture.componentRef.setInput('locations', mockTree);
      fixture.detectChanges();

      const flat = component.flatLocations();
      expect(flat[0].path).toBe(''); // Garage — root, no path
      expect(flat[1].path).toBe('Garage'); // Shelf A — parent is Garage
      expect(flat[2].path).toBe('Garage > Shelf A'); // Box 1
      expect(flat[3].path).toBe(''); // Kitchen — root
    });

    it('should handle empty tree', () => {
      fixture.componentRef.setInput('locations', []);
      fixture.detectChanges();

      expect(component.flatLocations().length).toBe(0);
    });
  });

  describe('filter', () => {
    beforeEach(() => {
      fixture.componentRef.setInput('locations', mockTree);
      fixture.detectChanges();
    });

    it('should show all locations when query is empty', () => {
      expect(component.filteredLocations().length).toBe(4);
    });

    it('should filter by name (case-insensitive)', () => {
      component.query.set('shelf');
      expect(component.filteredLocations().length).toBe(1);
      expect(component.filteredLocations()[0].name).toBe('Shelf A');
    });

    it('should filter by partial match', () => {
      component.query.set('box');
      expect(component.filteredLocations().length).toBe(1);
      expect(component.filteredLocations()[0].name).toBe('Box 1');
    });

    it('should return empty when no match', () => {
      component.query.set('zzz');
      expect(component.filteredLocations().length).toBe(0);
    });
  });

  describe('select', () => {
    beforeEach(() => {
      fixture.componentRef.setInput('locations', mockTree);
      fixture.detectChanges();
    });

    it('should emit selection when item is selected', () => {
      spyOn(component.locationSelected, 'emit');

      const flat = component.flatLocations();
      component.selectItem(flat[1]); // Shelf A

      expect(component.locationSelected.emit).toHaveBeenCalledWith({
        id: 'loc-1-1',
        name: 'Shelf A'
      });
    });

    it('should set selectedLocation on select', () => {
      const flat = component.flatLocations();
      component.selectItem(flat[0]);

      expect(component.selectedLocation()).toBeTruthy();
      expect(component.selectedLocation()!.id).toBe('loc-1');
    });

    it('should close dropdown after selection', () => {
      component.isOpen.set(true);
      const flat = component.flatLocations();
      component.selectItem(flat[0]);

      expect(component.isOpen()).toBe(false);
    });
  });

  describe('clear', () => {
    beforeEach(() => {
      fixture.componentRef.setInput('locations', mockTree);
      fixture.detectChanges();
      // Pre-select
      component.selectItem(component.flatLocations()[0]);
    });

    it('should emit null when cleared', () => {
      spyOn(component.locationSelected, 'emit');
      component.clearSelection();
      expect(component.locationSelected.emit).toHaveBeenCalledWith(null);
    });

    it('should clear selectedLocation', () => {
      component.clearSelection();
      expect(component.selectedLocation()).toBeNull();
    });
  });

  describe('pre-select', () => {
    it('should pre-select location by ID on init', () => {
      fixture.componentRef.setInput('locations', mockTree);
      fixture.componentRef.setInput('selectedLocationId', 'loc-1-1');
      fixture.detectChanges();

      expect(component.selectedLocation()).toBeTruthy();
      expect(component.selectedLocation()!.name).toBe('Shelf A');
    });

    it('should not pre-select if ID does not match', () => {
      fixture.componentRef.setInput('locations', mockTree);
      fixture.componentRef.setInput('selectedLocationId', 'nonexistent');
      fixture.detectChanges();

      expect(component.selectedLocation()).toBeNull();
    });
  });

  describe('display value', () => {
    beforeEach(() => {
      fixture.componentRef.setInput('locations', mockTree);
      fixture.detectChanges();
    });

    it('should show selected location name when dropdown is closed', () => {
      component.selectItem(component.flatLocations()[1]);
      expect(component.displayValue()).toBe('Shelf A');
    });

    it('should show query when dropdown is open', () => {
      component.isOpen.set(true);
      component.query.set('gar');
      expect(component.displayValue()).toBe('gar');
    });

    it('should show empty string when nothing selected and dropdown closed', () => {
      expect(component.displayValue()).toBe('');
    });
  });

  describe('no-results', () => {
    it('should show no results message in template when filter matches nothing', () => {
      fixture.componentRef.setInput('locations', mockTree);
      fixture.detectChanges();

      component.query.set('zzz');
      component.isOpen.set(true);
      fixture.detectChanges();

      const noResults = fixture.nativeElement.querySelector('.loc-autocomplete__no-results');
      expect(noResults).toBeTruthy();
      expect(noResults.textContent.trim()).toBe('No locations found');
    });
  });

  describe('keyboard navigation', () => {
    beforeEach(() => {
      fixture.componentRef.setInput('locations', mockTree);
      fixture.detectChanges();
      component.isOpen.set(true);
    });

    it('should navigate down with ArrowDown', () => {
      const event = new KeyboardEvent('keydown', { key: 'ArrowDown' });
      component.onKeydown(event);
      expect(component.highlightedIndex()).toBe(0);

      component.onKeydown(event);
      expect(component.highlightedIndex()).toBe(1);
    });

    it('should navigate up with ArrowUp', () => {
      component.highlightedIndex.set(2);
      const event = new KeyboardEvent('keydown', { key: 'ArrowUp' });
      component.onKeydown(event);
      expect(component.highlightedIndex()).toBe(1);
    });

    it('should wrap around on ArrowDown at end', () => {
      component.highlightedIndex.set(3); // last item (4 total)
      const event = new KeyboardEvent('keydown', { key: 'ArrowDown' });
      component.onKeydown(event);
      expect(component.highlightedIndex()).toBe(0);
    });

    it('should close dropdown on Escape', () => {
      const event = new KeyboardEvent('keydown', { key: 'Escape' });
      component.onKeydown(event);
      expect(component.isOpen()).toBe(false);
    });

    it('should select highlighted item on Enter', () => {
      spyOn(component.locationSelected, 'emit');
      component.highlightedIndex.set(1); // Shelf A
      const event = new KeyboardEvent('keydown', { key: 'Enter' });
      component.onKeydown(event);

      expect(component.locationSelected.emit).toHaveBeenCalledWith({
        id: 'loc-1-1',
        name: 'Shelf A'
      });
    });
  });

  describe('validation', () => {
    it('should show error when required and no selection after touch', () => {
      fixture.componentRef.setInput('locations', mockTree);
      fixture.componentRef.setInput('required', true);
      fixture.detectChanges();

      component.touched.set(true);
      fixture.detectChanges();

      const error = fixture.nativeElement.querySelector('.loc-autocomplete__error');
      expect(error).toBeTruthy();
      expect(error.textContent.trim()).toBe('Location is required');
    });

    it('should not show error when not required', () => {
      fixture.componentRef.setInput('locations', mockTree);
      fixture.componentRef.setInput('required', false);
      fixture.detectChanges();

      component.touched.set(true);
      fixture.detectChanges();

      const error = fixture.nativeElement.querySelector('.loc-autocomplete__error');
      expect(error).toBeFalsy();
    });

    it('should not show error when selection exists', () => {
      fixture.componentRef.setInput('locations', mockTree);
      fixture.componentRef.setInput('required', true);
      fixture.detectChanges();

      component.touched.set(true);
      component.selectItem(component.flatLocations()[0]);
      fixture.detectChanges();

      const error = fixture.nativeElement.querySelector('.loc-autocomplete__error');
      expect(error).toBeFalsy();
    });
  });
});
