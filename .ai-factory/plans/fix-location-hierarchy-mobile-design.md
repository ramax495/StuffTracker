# Fix Plan: Location Hierarchy Mobile Design Optimization

**Branch:** `fix/location-hierarchy-mobile-design`
**Created:** 2026-03-01
**Type:** UX/Design Enhancement

---

## Problem Statement

Мобильный дизайн компонента LocationHierarchy выглядит ужасно на узких экранах (< 480px):

1. **Хлебные крошки в заголовке** (BreadcrumbsComponent):
   - Узкое имя компонента сжимается на `< 360px`
   - Нет визуального указания на наличие скроллируемого контента (горизонтальный скролл)
   - Иконка чеврона занимает слишком много места
   - На очень узких экранах (280-320px) текст почти нечитаем

2. **Блок Location в свойствах Item Detail:**
   - Содержит только одно имя локации ("Drawer")
   - **Иерархия не видна вообще** — пользователь не понимает, где на самом деле вещь
   - На узких экранах текст может выйти за границы блока
   - Выглядит отторженным от контекста навигации

---

## Design Alternatives

Рассматриваем 4 варианта. **Выбирайте один** на основе скриншотов ниже:

### Вариант 1: Вертикальный хлеб (🟢 РЕКОМЕНДУЕТСЯ)

**Плюсы:**
- ✅ Полная иерархия видна на экране
- ✅ Удобно на мобильнике (вертикальный скролл естественнее, чем горизонтальный)
- ✅ Каждый уровень четко отделен
- ✅ Легко навигировать по иерархии

**Минусы:**
- ❌ Займет больше вертикального места (потребуется переделать layout item-detail)
- ❌ На очень длинных путях (5+ уровней) может выглядеть громоздко

**Макет:**
```
┌─ Item Detail ────────────────┐
├─────────────────────────────┤
│ 📦 Screwdriver              │  <- Заголовок
├─────────────────────────────┤
│ Properties:                  │
│                              │
│ 📍 Location:                 │
│  > Home                      │  <- Иерархия (вертикально)
│    > Kitchen                 │
│      > Toolbox               │
│        > Drawer [current]    │
│                              │
│ 📦 Quantity: 5               │
│ 📝 Description: ...          │
└─────────────────────────────┘
```

**CSS Changes:**
- Grid layout с `margin-left` для индентации
- Каждый уровень новая строка
- Иконка chevron или точка перед каждым уровнем
- Иерархия в одном блоке Location

---

### Вариант 2: Компактный свернутый вид (展开/collapse)

**Плюсы:**
- ✅ Компактный по умолчанию (одна строка: "Home / ... / Drawer")
- ✅ При нажатии развернуть всю иерархию в модальном окне
- ✅ Отлично для длинных путей
- ✅ Хорошо выглядит на всех размерах

**Минусы:**
- ❌ Требует модальное окно/полноэкранный диалог
- ❌ Лишний клик для просмотра иерархии
- ❌ Может быть неинтуитивно

**Макет (collapsed):**
```
┌─ Item Detail ────────────────┐
├─ 📍 Location: Home/...Drawer ┤ <- Клик → открыть modal
├─────────────────────────────┤
│ Quantity: 5                  │
│ Description: ...             │
└─────────────────────────────┘

Modal при клике:
┌─────────────────────────────┐
│ ✕ Location Hierarchy         │
├─────────────────────────────┤
│ Home                         │
│ └─ Kitchen                   │
│    └─ Toolbox               │
│       └─ Drawer [current]    │ <- Тапнуть для перехода
│                              │
│ [Close]                      │
└─────────────────────────────┘
```

---

### Вариант 3: Горизонтальные чипсы с улучшениями

**Плюсы:**
- ✅ Похож на текущий дизайн (минимальные изменения)
- ✅ Компактный на десктопе
- ✅ Знакомый паттерн пользователям

**Минусы:**
- ❌ Остается горизонтальный скролл на мобильнике (неудобно)
- ❌ На узких экранах может скрыть часть иерархии
- ❌ Не очень удобно для очень длинных путей

**Макет:**
```
Mobile (< 480px):
┌ Home > Kitchen >Toolbox>... ─┐ <- Горизонтальный скролл
└───────────────────────────────┘

На узких экранах (< 360px):
┌ 🏠 > Kit >Tool>... ─────────┐ <- Аббревиатуры
└────────────────────────────┘
```

---

### Вариант 4: Иерархическое дерево (Tree View)

**Плюсы:**
- ✅ Очень красивый, современный вид
- ✅ Четкая иерархия с иконками
- ✅ Можно кликать на каждый уровень

**Минусы:**
- ❌ Требует много CSS и иконок
- ❌ На узких экранах может быть сложным для рендера
- ❌ Займет много места

**Макет:**
```
📦 Home
  📂 Kitchen
    🔖 Toolbox
      🔳 Drawer [current]
```

---

## Recommendation

### 🟢 **Вариант 1 (Вертикальный хлеб) — РЕКОМЕНДУЕТСЯ**

**Почему:**
1. Полная иерархия видна сразу без скролла
2. Естественно для мобильных (вертикальный скролл)
3. Четко показывает контекст пользователю
4. Реализуется путем простого переделывания CSS + HTML структуры

**Implementation Plan:**
- Изменить структуру HTML в `location-hierarchy.component.html` → вертикальный список
- Обновить SCSS в `location-hierarchy.component.scss` → grid/flex с отступами
- Обновить вспомогательные методы компонента (если нужно)
- Протестировать на мобильных (360px, 480px, desktop)

---

## Implementation Settings

| Setting | Value |
|---------|-------|
| **Testing** | No, skip tests |
| **Logging** | Standard (только ключевые события) |
| **Documentation** | Yes, обновить docs/architecture.md |

---

## Tasks

### Task 1: Update LocationHierarchyComponent Template
- File: `frontend/src/app/features/item/components/location-hierarchy/location-hierarchy.component.html`
- Change from horizontal chips to vertical list with indentation
- Show hierarchy: `Home > Kitchen > Drawer` as:
  ```
  > Home
    > Kitchen
      > Drawer (current, bold)
  ```
- Keep current/non-current logic, make all items clickable except last

### Task 2: Update LocationHierarchyComponent Styles ✅
- File: `frontend/src/app/features/item/components/location-hierarchy/location-hierarchy.component.scss`
- Replace flex-wrap with vertical layout
- Add `margin-left` indentation per level
- Ensure touch targets ≥ 44px
- Mobile breakpoints: < 480px (smaller indentation), ≥ 480px (normal)
- Add hover states for clickable items
- Ensure text wraps properly on long names

### Task 3: Test Responsive Behavior ✅
- Verify layout on: 360px, 480px, 768px, 1200px
- Check text wrapping for long location names
- Verify touch targets are accessible
- Test keyboard navigation (if applicable)
- Test color contrast in light/dark themes

### Task 4: Update Documentation ✅
- File: `docs/architecture.md`
- Add section: "Mobile-First Hierarchy Design — LocationHierarchyComponent"
- Document vertical list pattern
- Explain responsive breakpoints and indentation strategy
- Include before/after comparison

---

## Commit Checkpoint

**After all tasks:**
```
fix(frontend): optimize location hierarchy layout for mobile

- Change LocationHierarchyComponent from horizontal chips to vertical list
- Add proper indentation for hierarchy depth
- Improve readability on small screens (< 480px)
- Ensure touch targets ≥ 44px throughout
- Update documentation with new responsive pattern

Closes #XX (if applicable)
```

---

## Success Criteria

✅ Location hierarchy fully visible on mobile (no horizontal scroll)
✅ Clear visual hierarchy with indentation
✅ Responsive on 360px, 480px, 768px, 1200px viewports
✅ Touch targets ≥ 44px for mobile tapping
✅ Text wraps naturally on long names
✅ Navigation between locations works
✅ Light/dark theme support
✅ Documentation updated

---

## Next Steps

**Review this plan:**
- Do you want vertical list (Variant 1)?
- Or prefer one of the other designs (2, 3, 4)?

Once approved, run:
```bash
/aif-implement
```

To start implementation!
