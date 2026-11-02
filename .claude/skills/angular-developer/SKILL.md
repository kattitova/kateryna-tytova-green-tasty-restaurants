---
name: angular-developer
description: Generates Angular code and provides architectural guidance for frontend/restaurant-frontend-app (Angular 22). Trigger when creating components, services, guards, or HTTP communication, or for best practices on reactivity (signals, RxJS), forms, dependency injection, routing, accessibility (ARIA), animations, styling, testing, naming conventions, or CLI tooling.
license: MIT
metadata:
  author: Copyright 2026 Google LLC
  version: '1.0'
---

<!--
Source: https://github.com/angular/angular/tree/main/skills/dev-skills/angular-developer
(commit fdb4517612cf), MIT License, Copyright Google LLC. See LICENSE in this folder.
Adapted for marathon2026 (frontend/restaurant-frontend-app, Angular 22):
- Added "Project overrides" below. They win over the generic guidance and the references/ files.
- Replaced the `ng build` step: a production build here needs BACKEND_URL.
- Removed "Creating New Projects" (the app already exists).
- Testing: Karma + Jasmine, not Vitest. Forms: match the existing form. Signals: target, not current.
The references/ folder is unchanged upstream content. Read it through the overrides.
-->

# Angular Developer Guidelines

## Project overrides (read first)

The app's own rules are in `frontend/restaurant-frontend-app/CLAUDE.md`. That file wins over this skill. The most important differences from generic modern-Angular advice:

| Generic guidance in this skill | This project today | What to do |
|---|---|---|
| Signal `input()` / `output()`, `computed`, `linkedSignal`, `resource`, `httpResource` | 87 `@Input()`, 51 `@Output()`, `HttpClient` services. Migration is tracked in issue #6. | Use the decorator style that the surrounding code uses. Use signal APIs only in the task that owns the migration, or when the user asks. |
| `OnPush`, zoneless | All components use `ChangeDetectionStrategy.Eager`. `zone.js` is still loaded (issue #31). | Do not change change detection or remove zone.js in unrelated work. |
| Signal Forms for new forms | Reactive forms (10 files) and template-driven forms (11 files). No Signal Forms. | Match the form style of the feature. Ask before you introduce Signal Forms. |
| Vitest (`references/testing-fundamentals.md`) | Karma + Jasmine (`npm test`). | Use Jasmine APIs (`jasmine.createSpyObj`, `spyOn`, `toHaveBeenCalledWith`). Ignore Vitest-specific code. |
| Suffixless file names ("Intent over Role") | `*.component.ts`, `*.service.ts`, `*.model.ts`. `angular.json` schematics keep the suffixes. | Keep the suffixes. `ng generate` already produces them. |
| SSR, hydration, prerendering | No SSR. Static SPA on S3 + CloudFront. | Ignore SSR references. |
| Tailwind CSS | SCSS with `@use`. No Tailwind. | Do not add Tailwind. |
| `@if` / `@for` control flow | Already used everywhere (no `*ngIf`). | Keep using built-in control flow. |
| Add libraries freely | Bundle is near the 1 MB error budget. | Do not add npm packages without asking. |

Apply the generic guidance below where it does not conflict with this table: `inject()`, standalone components, built-in control flow, typed HTTP, accessibility, route guards, `takeUntilDestroyed()`.

1. The project uses Angular 22 (`package.json`). Check the version before you use an API that is newer.

2. When generating code, follow Angular's style guide and best practices for maintainability and performance. Use the Angular CLI for scaffolding components, services, directives, pipes, and routes to ensure consistency.

3. Once you finish generating code, run the verification gates from `frontend/restaurant-frontend-app/CLAUDE.md` §6. A plain `ng build` / `npm run build` fails here without `BACKEND_URL`:

   ```bash
   npx tsc -p tsconfig.app.json  --noEmit
   npx tsc -p tsconfig.spec.json --noEmit
   BACKEND_URL=https://example.com npm run build   # must end with "Bundle check passed"
   npm test -- --watch=false
   ```

   If there are errors, analyze the error messages and fix them before proceeding. Do not skip this step.

## Components

When working with Angular components, consult the following references based on the task:

- **Fundamentals**: Anatomy, metadata, core concepts, self-closing tags, and template control flow (@if, @for, @switch). Read [components.md](references/components.md)
- **Inputs**: Signal-based inputs, transforms, and model inputs. Read [inputs.md](references/inputs.md)
- **Outputs**: Signal-based outputs and custom event best practices. Read [outputs.md](references/outputs.md)
- **Host Elements**: Host bindings and attribute injection. Read [host-elements.md](references/host-elements.md)
- **Naming Conventions**: Modern Angular v20+ naming style ("Intent over Role") for files, components, services, directives, pipes, and models. Read [naming-conventions.md](references/naming-conventions.md)
- **Queries and Content Projection**: Signal-based view and content queries (viewChild, viewChildren, contentChild, contentChildren) and content projection with `<ng-content>` slots and fallback content. Read [queries-and-projection.md](references/queries-and-projection.md)
- **Deferrable Views**: Lazy loading, code splitting, triggers, prefetching, loading states, SSR, and layout stability with `@defer`. Read [deferrable-views.md](references/deferrable-views.md)

If you require deeper documentation not found in the references above, read the documentation at `https://angular.dev/guide/components`.

## Reactivity and Data Management

When managing state and data reactivity, use Angular Signals and consult the following references:

- **Signals Overview**: Core signal concepts (`signal`, `computed`), reactive contexts, and `untracked`. Read [signals-overview.md](references/signals-overview.md)
- **Dependent State (`linkedSignal`)**: Creating writable state linked to source signals. Read [linked-signal.md](references/linked-signal.md)
- **Async Reactivity (`resource`)**: Fetching asynchronous data directly into signal state. Read [resource.md](references/resource.md)
- **Side Effects (`effect`)**: Logging, third-party DOM manipulation (`afterRenderEffect`), and when NOT to use effects. Read [effects.md](references/effects.md)

## HTTP Communication

When communicating with backend services, use Angular HTTP APIs and consult the following reference:

- **HTTP Client and Resources**: `provideHttpClient`, `HttpClient`, interceptors, and `httpResource`. Read [http-client.md](references/http-client.md)

## Forms

This project uses reactive forms and template-driven forms (see "Project overrides"). Use the form type that the feature already uses. For a new complex form, use reactive forms. Ask before you introduce Signal Forms.

- **Signal Forms**: Use signals for form state management. Read [signal-forms.md](references/signal-forms.md)
- **Template-driven forms**: Use for simple forms. Read [template-driven-forms.md](references/template-driven-forms.md)
- **Reactive forms**: Use for complex forms. Read [reactive-forms.md](references/reactive-forms.md)

## Dependency Injection

When implementing dependency injection in Angular, follow these guidelines:

- **Fundamentals**: Overview of Dependency Injection, services, and the `inject()` function. Read [di-fundamentals.md](references/di-fundamentals.md)
- **Creating and Using Services**: Creating services, the `providedIn: 'root'` option, and injecting into components or other services. Read [creating-services.md](references/creating-services.md)
- **Defining Dependency Providers**: Automatic vs manual provision, `InjectionToken`, `useClass`, `useValue`, `useFactory`, and scopes. Read [defining-providers.md](references/defining-providers.md)
- **Injection Context**: Where `inject()` is allowed, `runInInjectionContext`, and `assertInInjectionContext`. Read [injection-context.md](references/injection-context.md)
- **Hierarchical Injectors**: The `EnvironmentInjector` vs `ElementInjector`, resolution rules, modifiers (`optional`, `skipSelf`), and `providers` vs `viewProviders`. Read [hierarchical-injectors.md](references/hierarchical-injectors.md)

## Pipes

When formatting values in templates, creating custom pipes, or reusing pipe-like logic in TypeScript, consult the following reference. Prefer pipes in templates; outside templates, avoid injecting pipe classes just to call `transform()`.

- **Pipes**: Built-in pipe imports, custom pipe naming and implementation, pure vs impure pipes, and TypeScript reuse patterns using standalone formatting functions or extracted plain functions. Read [pipes.md](references/pipes.md)

## Angular Aria

When building accessible custom components for any of the following patterns: Accordion, Listbox, Combobox, Menu, Tabs, Toolbar, Tree, Grid, consult the following reference:

- **Angular Aria Components**: Building headless, accessible components (Accordion, Listbox, Combobox, Menu, Tabs, Toolbar, Tree, Grid) and styling ARIA attributes. Read [angular-aria.md](references/angular-aria.md)

## Routing

When implementing navigation in Angular, consult the following references:

- **Define Routes**: URL paths, static vs dynamic segments, wildcards, and redirects. Read [define-routes.md](references/define-routes.md)
- **Route Loading Strategies**: Eager vs lazy loading, and context-aware loading. Read [loading-strategies.md](references/loading-strategies.md)
- **Show Routes with Outlets**: Using `<router-outlet>`, nested outlets, and named outlets. Read [show-routes-with-outlets.md](references/show-routes-with-outlets.md)
- **Navigate to Routes**: Declarative navigation with `RouterLink` and programmatic navigation with `Router`. Read [navigate-to-routes.md](references/navigate-to-routes.md)
- **Control Route Access with Guards**: Implementing `CanActivate`, `CanMatch`, and other guards for security. Read [route-guards.md](references/route-guards.md)
- **Data Resolvers**: Pre-fetching data before route activation with `ResolveFn`. Read [data-resolvers.md](references/data-resolvers.md)
- **Router Lifecycle and Events**: Chronological order of navigation events and debugging. Read [router-lifecycle.md](references/router-lifecycle.md)
- **Rendering Strategies**: Choosing between CSR, SSG (prerendering), and SSR. Read [rendering-strategies.md](references/rendering-strategies.md)
- **Server and Hybrid Rendering**: Implementing server routes, prerendering, hydration, server-compatible code, and request data in an SSR-configured app. Read [server-side-rendering.md](references/server-side-rendering.md)
- **Route Transition Animations**: Enabling and customizing the View Transitions API. Read [route-animations.md](references/route-animations.md)

If you require deeper documentation or more context, visit the [official Angular Routing guide](https://angular.dev/guide/routing).

## Styling and Animations

When implementing styling and animations in Angular, consult the following references:

- **Using Tailwind CSS with Angular**: Integrating Tailwind CSS into Angular projects. Read [tailwind-css.md](references/tailwind-css.md)
- **Angular Animations**: Using native CSS (recommended) or the legacy DSL for dynamic effects. Read [angular-animations.md](references/angular-animations.md)
- **Styling components**: Best practices for component styles and encapsulation. Read [component-styling.md](references/component-styling.md)

## Testing

When writing or updating tests, consult the following references based on the task:

- **Fundamentals**: Best practices for unit testing, async patterns, and `TestBed`. Read [testing-fundamentals.md](references/testing-fundamentals.md). The reference uses Vitest. This project uses Karma + Jasmine, so translate `vi.fn()` to `jasmine.createSpy()` and `vi.spyOn` to `spyOn`. Use the provider patterns and coverage table in the app `CLAUDE.md` §6.
- **Component Harnesses**: Standard patterns for robust component interaction. Read [component-harnesses.md](references/component-harnesses.md)
- **Router Testing**: Using `RouterTestingHarness` for reliable navigation tests. Read [router-testing.md](references/router-testing.md)
- **End-to-End (E2E) Testing**: Setting up and running E2E tests. Read [e2e-testing.md](references/e2e-testing.md)

## Tooling

When working with Angular tooling, consult the following references:

- **Angular CLI**: Creating applications, generating code (components, routes, services), serving, and building. Read [cli.md](references/cli.md)
- **Code Modernization**: Automatically refactoring to modern standards using migrations. Read [migrations.md](references/migrations.md)
- **Angular MCP Server**: Available tools, configuration, and experimental features. Read [mcp.md](references/mcp.md)
- **Environment Configuration**: Strategies for build-time and runtime configuration. Read [environment-configuration.md](references/environment-configuration.md)
