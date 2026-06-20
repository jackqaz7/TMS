# RelayCommand.cs — explained

**Location**: `src/TMS_WPF_UI/Helpers/RelayCommand.cs`
**Purpose**: Generic `ICommand` implementation used to bind button clicks (and other UI actions) in XAML to methods in a ViewModel, following the MVVM pattern.

---

## Line-by-line

### 1. `using System;` / `using System.Windows.Input;`
- **Functional**: Imports needed for basic types and WPF's command infrastructure.
- **Technical**: `System.Windows.Input` is where `ICommand` and `CommandManager` live — both used later in this file.

### 2. `public class RelayCommand : ICommand`
- **Functional**: Defines a reusable "command" class — a generic wrapper that turns any method into something a WPF button can bind to.
- **Technical**: Implements the `ICommand` interface, meaning this class must provide `Execute`, `CanExecute`, and `CanExecuteChanged` — the three members `ICommand` requires.

### 3. `private readonly Action<object> _execute;`
- **Functional**: Stores "the actual work to do when the command runs" — e.g. login logic.
- **Technical**: `Action<object>` is a delegate type — a method that takes one `object` parameter and returns `void`. `private readonly` means this field is set once (in the constructor) and never reassigned, and is hidden from outside the class.

### 4. `private readonly Predicate<object> _canExecute;`
- **Functional**: Stores "the rule for whether the command is currently allowed to run" — e.g. "only if username and password aren't empty."
- **Technical**: `Predicate<object>` is a delegate type — a method taking `object`, returning `bool`. Equivalent to `Func<object, bool>` with a more semantically-named alias ("predicate" = a true/false test).

### 5. `public RelayCommand(Action<object> execute, Predicate<object> canExecute = null)`
- **Functional**: The constructor — when you write `new RelayCommand(ExecuteLogin, CanExecuteLogin)`, this runs, storing both methods for later use.
- **Technical**: `canExecute = null` is a default parameter — `new RelayCommand(ExecuteLogin)` is valid and `canExecute` defaults to `null`.

### 6. `_execute = execute ?? throw new ArgumentNullException(nameof(execute));`
- **Functional**: The `execute` method is mandatory — if it's missing, fail immediately with a clear error instead of crashing later in a confusing way.
- **Technical**: `??` is the null-coalescing operator — if `execute` is `null`, throw `ArgumentNullException` instead of assigning `null`. `nameof(execute)` gives `"execute"` as a string for the error message (avoids hardcoding the name as a literal string).

### 7. `_canExecute = canExecute;`
- **Functional**: Saves the "is it allowed to run" rule, or `null` if none was given.
- **Technical**: Simple field assignment — no validation, since `null` is a valid and expected value here.

### 8. `public bool CanExecute(object parameter)`
- **Functional**: Answers "can this command run right now?" — used by WPF to enable/disable bound buttons automatically.
- **Technical**: Part of the `ICommand` interface contract; returns `bool`, called by WPF's `CommandManager` whenever it re-evaluates command states.

### 9. `return _canExecute == null || _canExecute(parameter);`
- **Functional**: If no rule was given, always allow the command to run; otherwise, run the rule and use its answer.
- **Technical**: Short-circuit OR (`||`) — if `_canExecute == null` is `true`, `_canExecute(parameter)` is never evaluated, avoiding a call on a `null` delegate.

### 10. `public void Execute(object parameter)`
- **Functional**: Runs the actual command logic — this is what happens when the button is clicked.
- **Technical**: Part of the `ICommand` interface contract; returns `void`, invoked by WPF when the bound UI element triggers the command.

### 11. `_execute(parameter);`
- **Functional**: Hands off to whatever method was passed into the constructor (e.g. login logic).
- **Technical**: Invokes the stored `Action<object>` delegate, passing through the parameter received from the UI.

### 12. `public event EventHandler CanExecuteChanged { ... }`
- **Functional**: Lets WPF know "re-check `CanExecute`" whenever something in the app might have changed (e.g. user typed in a textbox).
- **Technical**: Part of the `ICommand` interface contract — a custom event implementation using `add`/`remove` accessors instead of a plain event field.

### 13. `add { CommandManager.RequerySuggested += value; }`
- **Functional**: When WPF subscribes to this command's "should I re-check?" notifications, piggyback on WPF's existing global notification system.
- **Technical**: `CommandManager.RequerySuggested` is a static event built into WPF that fires on common UI activity (focus change, keypress, etc.) — `add` hooks the incoming subscriber into that global event.

### 14. `remove { CommandManager.RequerySuggested -= value; }`
- **Functional**: When WPF unsubscribes, remove it from that same global notification system.
- **Technical**: Unhooks the subscriber from `CommandManager.RequerySuggested`, preventing memory leaks from dangling event handlers.

---

## Interview questions this answers

- **Q: How does MVVM connect a button click to your code without a `Click` event handler?**
  A: Via `ICommand` — the button's `Command` property binds to an `ICommand` property on the ViewModel (e.g. `LoginCommand`). `RelayCommand` is a generic, reusable implementation so you don't write a new `ICommand` class for every action.

- **Q: How does WPF know when to enable/disable a bound button?**
  A: WPF calls `CanExecute()` whenever `CommandManager.RequerySuggested` fires (on focus changes, keypresses, etc.). `RelayCommand` wires its `CanExecuteChanged` event directly to that global event, so it doesn't need to manually raise notifications itself.

- **Q: Why use `??` with `ArgumentNullException` instead of just letting it fail later?**
  A: Fail-fast principle — if `_execute` were `null`, the error would only surface when `Execute()` is called (a `NullReferenceException` deep in the call stack). Validating in the constructor gives an immediate, clear error pointing at the actual misuse.

- **Q: What's the difference between `Action<object>` and `Predicate<object>`?**
  A: Both are delegate types. `Action<object>` takes an `object` and returns `void` (used for "do something"). `Predicate<object>` takes an `object` and returns `bool` (used for "is this true?"). They're used here for the command's action and its enable/disable rule respectively.

---

## Things I still need to revisit

- [ ] Why does `CanExecuteChanged` use `add`/`remove` instead of a plain `event EventHandler CanExecuteChanged;`?
- [ ] What would break if `CommandManager.RequerySuggested` weren't used — i.e. why not raise `CanExecuteChanged` manually inside `RelayCommand`?
