# "Qué comemos" — UI del menú principal (UI Toolkit)

Migración solo de la **UI** (UXML + USS) de tu `index.html` a Unity UI Toolkit,
organizada estilo atómico. Todavía no hay lógica en C# ni conexión a Firebase:
eso lo haremos en el siguiente paso.

## Cómo instalarlo en tu proyecto Unity 6.6

1. Copia toda la carpeta `UI/MainMenu/` dentro de `Assets/UI/` de tu proyecto
   (o donde prefieras; solo tendrás que reajustar las rutas relativas de los
   `<Style src="...">` y `<ui:Template src="...">` si cambias la profundidad).
2. Unity generará automáticamente el `.meta` de cada `.uxml`/`.uss` al
   detectarlos.
3. Abre `MainMenu.uxml` con doble clic → se abre en **UI Builder**. Ahí verás
   ya montada toda la jerarquía: cabecera, navegación de semana, las 7
   tarjetas de día (con sus 2 slots de comida/cena cada una) y el panel de
   edición, todo con sus estilos aplicados.
4. Para verlo en pantalla en runtime: crea un `UIDocument` en un GameObject,
   asígnale `MainMenu.uxml` como *Source Asset* y un `PanelSettings` (si no
   tienes uno, créalo con clic derecho → Create → UI Toolkit → Panel Settings
   Asset).

## Estructura (atómico)

```
MainMenu/
├── MainMenu.uxml              ← PÁGINA: ensambla todo lo demás
├── README.md
├── Styles/
│   ├── Theme.uss              ← variables de color (dark/light), = :root del CSS
│   ├── Atoms.uss              ← botones, inputs, texto, badges, punto de color
│   ├── Molecules.uss          ← cabecera de día, meal-tag, cell-btn, menu-item...
│   ├── Organisms.uss          ← topbar, dropdown, week-nav, day-card, edit-panel
│   └── Page.uss                ← contenedor .wrap
└── Components/                 ← moléculas y organismos reutilizables (UXML)
    ├── MenuItem.uxml
    ├── CalendarMenuRow.uxml
    ├── DropdownMenu.uxml
    ├── HeaderBar.uxml
    ├── WeekNav.uxml
    ├── CellButton.uxml
    ├── MealSlot.uxml
    ├── DayCard.uxml
    ├── ReminderLine.uxml
    ├── EditPanel.uxml
    └── Toast.uxml
```

Cada `.uxml` de `Components/` es un **`ui:Template`** independiente: se puede
abrir y editar solo en UI Builder, y se instancia con `<ui:Instance>` donde
haga falta (p. ej. `DayCard.uxml` se instancia 7 veces en `MainMenu.uxml`).

## Qué componente HTML corresponde a qué archivo

| HTML original | Archivo Unity |
|---|---|
| `body.theme-light` / `:root` | `Styles/Theme.uss` |
| `.brand h1`, `.menu-btn`, `.nav-btn`, `.today-btn`, `.btn`(+variantes), inputs, `.calendar-owner-badge`, `.demo-badge`, `.guest-note` | `Styles/Atoms.uss` |
| `.day-card-head`, `.meal-tag`, `.cell-btn`(+selected), `.menu-item`(+active/danger), `.reminder-line` | `Styles/Molecules.uss` |
| `.header-row`, `.dropdown-menu`, `.week-nav`, `.day-card`, `.week-grid`, `.edit-panel`, `.toast` | `Styles/Organisms.uss` |
| `<header class="topbar">` | `Components/HeaderBar.uxml` |
| `#dropdownMenu` | `Components/DropdownMenu.uxml` |
| `.calendar-menu-row` | `Components/CalendarMenuRow.uxml` |
| `.week-nav` | `Components/WeekNav.uxml` |
| `.day-card` completa (cabecera + comida + cena) | `Components/DayCard.uxml` |
| `.meal-slot` (Comida o Cena) | `Components/MealSlot.uxml` |
| `.cell-btn` (celda "+ añadir" / plato guardado) | `Components/CellButton.uxml` |
| `.edit-panel` / `#editSlot` | `Components/EditPanel.uxml` |
| `.reminder-line` | `Components/ReminderLine.uxml` |
| `.toast` | `Components/Toast.uxml` |

## Decisiones y limitaciones a tener en cuenta (USS ≠ CSS 100%)

- **Tema día/noche**: en vez de una clase en `<body>`, la clase `theme-dark`
  / `theme-light` se pone en el `VisualElement` raíz (`#root` en
  `MainMenu.uxml`). Cambiarla en C# recalcula automáticamente todas las
  variables `var(--...)`, igual que en tu CSS.
- **`gap`**: USS no tiene la propiedad `gap` de Flexbox. Lo he resuelto con
  `margin-right`/`margin-bottom` en los hijos (p. ej. `.day-card` tiene
  `margin-right: 10px`). Si necesitas que el último elemento no tenga
  margen, añádele una clase modificadora desde C#.
- **`box-shadow`**: no está garantizado en todas las versiones de UI Toolkit;
  he dejado un comentario en `.dropdown-menu` por si tu versión de Unity 6.6
  ya lo soporta.
- **Responsive / media queries**: UI Toolkit no tiene `@media`. He añadido
  la clase `.week-grid--stacked` en `Organisms.uss` (equivalente a tu
  `@media max-width:600px`) para alternarla desde C# según el ancho del
  panel en runtime.
- **Inputs de fecha/hora**: UI Toolkit runtime no tiene un `<input
  type=date>` / `type=time` nativo. `ReminderLine.uxml` usa dos `TextField`
  de marcador de posición (`AAAA-MM-DD`, `HH:MM`); en el siguiente paso
  podemos crear un control propio o usar validación de texto.
- **Fuentes**: `Newsreader` (títulos, cursiva) y `Manrope` (texto) hay que
  importarlas como `Font Asset` en Unity y asignarlas donde pone
  `-unity-font-definition: none;` (en `Atoms.uss`, clases `.text-heading`,
  `.brand-title`, `.text-body`, y en los títulos `.edit-panel__title`,
  `.week-nav-label`, `.day-num`).
- **Estados dinámicos** (menú abierto/cerrado, celda con o sin plato,
  badges, tarjeta "hoy", texto de la etiqueta de semana, etc.) están todos
  montados en el UXML con su estado por defecto y clases modificadoras ya
  preparadas (`--selected`, `--today`, `--active`, `--danger`,
  `--show`...). Faltan por conectar con C#: eso es "poco a poco" el
  siguiente paso, cuando pasemos la lógica del `render()` del HTML.

## Siguiente paso sugerido

Cuando quieras seguimos con un `MainMenuController.cs` que:
1. Consulte los `VisualElement` por nombre (`root.Q<Button>("prev-week-btn")`, etc.).
2. Rellene las 7 `DayCard` con las fechas reales de la semana.
3. Muestre/oculte el panel de edición y el menú desplegable.
4. Conecte el toggle de tema y el toast.

Sin tocar aún Firebase ni el guardado de datos, tal como pediste.
