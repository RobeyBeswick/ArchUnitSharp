---
title: Layers
---

# Layers — a named-layer policy

The layers module expresses an N-layer policy: declare layers, assert rules over them, and check the
policy as a whole in one call.

## Declaring layers

`Layer(name)` begins a declaration, completed with `DefinedBy(glob)` (a whole-path glob) or
`DefinedByFolder(glob)` (a folder glob):

```csharp
Project.Layers()
    .Layer("App").DefinedByFolder("src/App")
    .Layer("Models").DefinedByFolder("src/Models")
    .Layer("Persistence").DefinedByFolder("src/Persistence");
```

## Asserting rules

`WhereLayer(name)` begins a rule over a declared layer, completed with one of the two moods:

- **`MayOnlyDependOnLayers(...)`** — the allowlist: the subject may depend only on the given layers.
  Intra-layer dependencies are always allowed, and with no arguments the layer is sealed — it may
  depend on no other layer at all.
- **`MayNotDependOnLayers(...)`** — the blocklist: the subject may not depend on any of the given
  layers.

```csharp
Project.Layers()
    .Layer("App").DefinedByFolder("src/App")
    .Layer("Models").DefinedByFolder("src/Models")
    .Layer("Persistence").DefinedByFolder("src/Persistence")
    .WhereLayer("App").MayOnlyDependOnLayers("Models")
    .WhereLayer("Models").MayOnlyDependOnLayers("Persistence")
    .WhereLayer("Persistence").MayOnlyDependOnLayers()
    .Check();
```

A policy accumulates declarations and rules and is checked as a whole with `Check(CheckOptions?)`;
an empty list of violations means every rule passed, and a policy with no rules passes. Blocklist
constraints are checked first, and a cross-layer dependency a blocklist already reported on a subject
layer is not re-reported by an allowlist on the same subject.

The chain reads as a sentence an architect could write: *layer App may only depend on layers Models*.

Next: [Slices](slices.md).