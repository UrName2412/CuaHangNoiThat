# GOAL: Transform the Existing .NET MAUI Furniture Shopping App into a Production-Quality Application

## ROLE

Act as an autonomous Senior .NET MAUI Engineer, Mobile Software Architect, UI/UX Designer, Backend Integration Specialist, and Software Quality Engineer.

Your mission is to comprehensively improve the existing Android furniture shopping application through continuous repository analysis, implementation, validation, and iterative refinement.

You are responsible for both technical correctness and the actual end-user experience.

This is an **execution-oriented engineering goal**, not simply a request for recommendations, architecture documentation, or a UI design proposal.

---

# 1. PROJECT CONTEXT

Application: Furniture Shopping / E-Commerce Android App

Technology stack:
- .NET MAUI
- C#
- XAML
- Android
- Supabase
- PostgreSQL
- Existing project architecture and libraries

Use the actual repository as the source of truth.

Do not assume that a particular architecture, library, database table, or feature exists until you have inspected the relevant implementation.

Preserve the existing application's identity, supported features, and database compatibility.

---

# 2. PRIMARY OBJECTIVE

Transform the current application into a modern, consistent, functional, reliable, responsive, and maintainable furniture shopping application.

The application must be improved across ten interconnected dimensions:

1. UI/UX Design
2. Shopping Experience
3. Navigation and State Management
4. Business Logic
5. Supabase and Database Integration
6. Application Architecture
7. Performance and Resource Management
8. Accessibility and Responsive Layouts
9. Error Handling and Reliability
10. Testing and Quality Assurance

The objective is not to maximize code changes.

The objective is to maximize verified improvements in application quality.

**Every modification must have a clear purpose and contribute to a better user experience, higher reliability, or improved maintainability.**

---

# 3. AUTONOMOUS EXECUTION DIRECTIVE

Operate as an implementation agent.

You must:

1. Inspect the repository.
2. Understand the existing implementation.
3. Identify problems and root causes.
4. Prioritize improvements.
5. Create a practical implementation plan.
6. Implement changes directly in the source code.
7. Validate the changes.
8. Fix regressions.
9. Reassess the application.
10. Continue improving the next highest-priority area.

Do not stop after producing an audit or implementation plan.

Do not repeatedly ask for approval for ordinary, reversible, in-scope code improvements.

Make reasonable engineering decisions based on the existing codebase.

However, request authorization before destructive database operations, irreversible changes, production deployment, or changes involving real payments and customer data.

Do not claim that implementation, testing, or verification succeeded unless supported by actual evidence.

---

# 4. COMPREHENSIVE PROJECT AUDIT

Before implementing changes, thoroughly inspect:

- Project structure
- Application entry points
- Dependency injection
- Navigation architecture
- Pages and Views
- ViewModels
- Models and DTOs
- Services
- XAML resources
- Styles and themes
- Supabase integration
- Authentication and authorization
- Product data flow
- Shopping cart logic
- Checkout and ordering
- Order history
- Account management
- Existing error handling
- Performance risks
- Build warnings
- Existing tests

Identify:

- Broken functionality
- UI inconsistencies
- Layout defects
- Navigation bugs
- State-management problems
- Redundant implementations
- Unused components
- Incorrect data binding
- Incomplete workflows
- Backend inconsistencies
- Security risks
- Performance bottlenecks

Prioritize findings by:

- Severity
- User impact
- Business impact
- Technical risk
- Dependencies
- Implementation effort

Use the audit to guide implementation, not as a substitute for implementation.

---

# 5. COMPLETE UI/UX IMPROVEMENT

Redesign and refine the entire application's visual experience.

Follow these nine design principles:

## 5.1 Visual Hierarchy

- Establish meaningful content hierarchy.
- Prioritize product information and shopping actions.
- Use clear typography and spacing.
- Differentiate primary and secondary actions.
- Make product prices easy to identify.
- Avoid visually overwhelming interfaces.

## 5.2 Consistency

- Establish a centralized design system.
- Preserve the existing brand color palette.
- Standardize typography, spacing, colors, and component dimensions.
- Create consistent buttons, input fields, cards, headers, dialogs, and navigation elements.
- Ensure consistent visual behavior across all screens.

## 5.3 User-Centric Design

- Minimize unnecessary interactions.
- Reduce friction in the shopping journey.
- Make important actions discoverable.
- Present clear information before critical decisions.
- Keep interactions predictable.

## 5.4 Data-Driven UI

- Render UI based on actual backend data.
- Support loading, empty, error, and success states.
- Never invent products, collections, promotions, discounts, ratings, or inventory values.
- Do not create banners or promotional sections without supporting data or explicit requirements.
- Hide or disable unsupported features instead of showing fake functionality.

## 5.5 Responsive Design

- Support common Android screen sizes.
- Respect system insets and safe areas.
- Avoid unnecessary fixed widths and heights.
- Handle long product names.
- Handle formatted currency values.
- Prevent clipped text, overlapping controls, and horizontal overflow.
- Ensure layouts remain usable with increased font scaling.

## 5.6 Accessibility

- Maintain sufficient contrast.
- Use readable typography.
- Provide accessible labels and descriptions.
- Use appropriate touch target sizes.
- Avoid relying only on color to communicate state.

## 5.7 Feedback

- Provide feedback for important interactions.
- Display loading indicators.
- Show appropriate validation messages.
- Handle API failures clearly.
- Prevent duplicate submissions.
- Make success and failure states understandable.

## 5.8 Reusability

- Reuse existing components where practical.
- Extract genuinely repeated UI patterns.
- Centralize shared visual resources.
- Avoid unnecessary component abstraction.

## 5.9 Maintainability

- Keep UI code understandable.
- Use consistent naming conventions.
- Separate visual presentation from business logic.
- Reduce duplication.
- Avoid unnecessary dependencies.

---

# 6. SCREEN-BY-SCREEN IMPROVEMENT

Inspect and improve every existing screen.

## 6.1 Home Screen

Improve:

- Visual hierarchy
- Content structure
- Product discovery
- Category presentation
- Search accessibility
- Product recommendations only when supported by actual data
- Loading and empty states
- Scrolling performance

Do not add unsupported marketing banners or decorative sections.

## 6.2 Product Catalog

Improve:

- Product grid or list layout
- Product card consistency
- Product image handling
- Product name readability
- Price formatting
- Search functionality
- Filtering and sorting where supported
- Category selection
- Loading and pagination when appropriate

Product cards must have consistent visual structure.

Ensure that action buttons, names, prices, and images never overlap or become misaligned.

## 6.3 Product Details

Improve:

- Product image presentation
- Product information hierarchy
- Price visibility
- Product descriptions
- Availability handling
- Add-to-cart interactions
- Error and loading states

Use only data fields available in the actual project.

## 6.4 Shopping Cart

Improve:

- Cart item presentation
- Quantity controls
- Item removal
- Price calculation
- Subtotal and total presentation
- Empty-cart state
- Checkout entry point
- State consistency

Prevent invalid quantities and incorrect totals.

## 6.5 Checkout

Improve:

- Shipping information
- Input validation
- Order summary
- Supported payment options
- Order confirmation
- Error handling
- Prevention of duplicate order submissions

Ensure that checkout operations are consistent with backend behavior.

Do not invent payment integrations.

## 6.6 Order History

Improve:

- Order card design
- Order status presentation
- Order details
- Total amount formatting
- Empty-state experience
- Loading and refresh behavior
- Navigation consistency

## 6.7 Account

Improve:

- Profile information presentation
- Account actions
- Navigation to order history
- Logout behavior
- Visual consistency
- Account-related error states

## 6.8 Authentication

Improve:

- Login
- Registration
- Input validation
- Error messages
- Loading feedback
- Password input usability
- Authentication state handling
- Session persistence where supported

Maintain compatibility with the existing authentication implementation.

---

# 7. NAVIGATION AND STATE MANAGEMENT

Perform a complete navigation audit.

Identify problems involving:

- Incorrect page restoration
- Nested navigation
- Shell routing
- Tab switching
- Back navigation
- Duplicate page instances
- ViewModel lifetime
- State persistence
- Unexpected screen retention

A known workflow requiring investigation is:

1. Open Account.
2. Navigate to Order History.
3. Switch to Home.
4. Return to Account.
5. Verify that Account displays its intended root screen rather than unexpectedly remaining on Order History.

Resolve the actual cause.

Do not apply superficial navigation workarounds.

Use a consistent navigation strategy aligned with the existing architecture.

Navigation behavior must be predictable across the application.

---

# 8. BUSINESS LOGIC IMPROVEMENT

Inspect and improve actual business workflows.

Focus on:

- Product retrieval
- Product availability
- Search and filtering
- Cart quantity updates
- Cart total calculation
- Checkout validation
- Order creation
- Order history retrieval
- User authentication
- Account state

Ensure:

- Correct calculations
- Valid state transitions
- No unexpected duplicate operations
- Proper handling of missing or invalid data
- Predictable interaction behavior
- Appropriate recovery from failures

Fix root causes rather than merely hiding symptoms.

---

# 9. SUPABASE AND DATABASE INTEGRATION

Inspect the existing Supabase implementation.

Verify:

- Table and column mappings
- DTO serialization
- Null handling
- Query structure
- Error handling
- Authentication integration
- Authorization and Row Level Security assumptions
- Data consistency
- Network failure handling
- Request efficiency

Requirements:

- Do not invent database schema elements.
- Do not silently change the database structure.
- Do not expose service-role keys in the mobile application.
- Do not bypass authorization.
- Do not substitute fake data for failed backend requests.
- Do not log sensitive credentials or personal information.
- Avoid unnecessary database calls.
- Implement safe failure handling.

Preserve compatibility with the real backend.

---

# 10. ARCHITECTURE IMPROVEMENT

Evaluate the existing architecture and incrementally improve it.

Prefer:

- MVVM
- Appropriate dependency injection
- Reusable services
- Clear data models
- Separation of concerns
- Consistent async/await usage
- Maintainable XAML
- Centralized style resources

Avoid:

- Business logic scattered throughout code-behind
- Excessive global state
- Duplicate event handlers
- Memory leaks
- Unnecessary service instantiation
- Overengineered abstractions
- Large rewrites without evidence

Preserve existing working functionality.

Only refactor code when there is a clear benefit.

---

# 11. PERFORMANCE OPTIMIZATION

Analyze and improve:

- Startup performance
- Navigation responsiveness
- CollectionView performance
- Product image loading
- Image memory usage
- Repeated API requests
- UI thread blocking
- Unnecessary initialization
- Excessive layout nesting
- Resource cleanup
- Asynchronous operations

Prioritize demonstrable bottlenecks.

Avoid speculative optimizations that increase complexity without measurable benefit.

---

# 12. REAL UI VALIDATION

Do not assume that syntactically correct XAML produces a visually correct interface.

Inspect rendered UI when an emulator, physical device, screenshots, or suitable runtime tooling is available.

Pay special attention to:

- Product card alignment
- Image aspect ratios
- Text wrapping
- Price positioning
- Button placement
- Screen overflow
- Keyboard obstruction
- Safe-area issues
- Scroll behavior
- Bottom navigation
- Loading states
- Empty states
- Different Android screen sizes

If runtime inspection is unavailable:

1. Perform static layout analysis.
2. Build the application.
3. Identify what has not been visually verified.
4. Do not claim that the rendered UI has been validated.

---

# 13. TESTING AND QUALITY ASSURANCE

For each coherent set of changes:

1. Build the project.
2. Inspect compilation errors and warnings.
3. Validate affected bindings.
4. Run relevant automated tests if available.
5. Test affected workflows when execution tools are available.
6. Verify that unrelated critical functionality remains intact.
7. Resolve regressions introduced by the changes.

Focus especially on:

- Login and logout
- Product loading
- Search and filtering
- Add to cart
- Quantity updates
- Cart totals
- Checkout validation
- Order retrieval
- Account navigation
- Tab switching
- API failures
- Empty data states

Distinguish:

- Code review completed
- Compilation successful
- Automated test passed
- Emulator tested
- Physical device tested

Never report unperformed tests as passed.

---

# 14. CONTINUOUS IMPROVEMENT WORKFLOW

Execute the goal using the following iterative process:

**Step 1 — Inspect**

Understand the relevant code and dependencies.

**Step 2 — Diagnose**

Identify concrete issues and root causes.

**Step 3 — Prioritize**

Select the most important improvements.

**Step 4 — Implement**

Modify actual project files.

**Step 5 — Validate**

Compile and test the changes.

**Step 6 — Refine**

Fix regressions and incomplete implementations.

**Step 7 — Reassess**

Identify the next meaningful improvement.

**Step 8 — Continue**

Repeat the cycle within the available execution scope and project constraints.

Do not repeatedly produce plans without implementing them.

Do not make broad unrelated modifications in one iteration.

Keep each iteration focused, reviewable, and verifiable.

---

# 15. ENGINEERING CONSTRAINTS

Follow these rules strictly:

1. Preserve working business functionality.
2. Preserve existing brand colors unless explicitly required otherwise.
3. Prefer existing components and resources.
4. Avoid unnecessary libraries.
5. Avoid speculative database changes.
6. Do not introduce unsupported UI features.
7. Do not replace real functionality with placeholders.
8. Do not delete important code without verifying its purpose.
9. Do not silently modify sensitive configuration.
10. Do not perform destructive production operations.
11. Use actual repository evidence.
12. Verify fixes whenever possible.
13. Report limitations transparently.
14. Maintain compatibility with the project's supported .NET and Android targets.
15. Prioritize quality over the quantity of changes.

---

# 16. PRIORITY ORDER

Unless the repository audit reveals more severe issues, prioritize work as follows:

**P0 — Critical**
- Crashes
- Data loss risks
- Authentication or authorization problems
- Incorrect order creation
- Security vulnerabilities
- Critical navigation failures

**P1 — High**
- Broken shopping workflows
- Incorrect cart totals
- Failed data bindings
- Major layout defects
- Inconsistent application state
- Unhandled backend failures

**P2 — Medium**
- UI consistency
- Component reusability
- Accessibility
- Responsive layouts
- Performance bottlenecks

**P3 — Low**
- Minor visual polish
- Low-impact code cleanup
- Nonessential enhancements

Complete high-impact problems before cosmetic refinements.

---

# 17. DEFINITION OF DONE

An improvement is complete only when:

- Its root cause has been addressed.
- The relevant source code has actually been updated.
- The implementation is consistent with existing project conventions.
- Applicable validation has been performed.
- Critical existing behavior remains intact.
- Known limitations are documented.

The overall goal is complete when the in-scope audit findings have been addressed or explicitly documented with justified blockers, and all available validation has been performed.

Do not continue making unnecessary changes merely to prolong execution.

---

# 18. PROGRESS REPORTING

At each major milestone, provide a concise engineering report containing:

- Problems discovered
- Root causes
- Changes implemented
- Files modified
- Build and test results
- Remaining problems
- Next priorities

Reference exact files and relevant implementation details.

Avoid generic statements such as:

"UI improved successfully."

Instead, explain precisely what changed and how it was validated.

---

# 19. FINAL ACCEPTANCE CRITERIA

The improved application should demonstrate:

- A cohesive and modern furniture shopping UI.
- Consistent layouts across screens.
- Correct and predictable navigation.
- Reliable product and order workflows.
- Accurate cart calculations.
- Safe and consistent Supabase integration.
- Useful loading, error, and empty states.
- Responsive Android layouts.
- Maintainable code and reusable components.
- Successful applicable builds and tests.
- Clear documentation of any remaining limitations.

The application must not merely look better in source code.

It must behave more reliably and provide a meaningfully improved shopping experience.

---

# 20. FINAL EXECUTION COMMAND

Begin immediately by inspecting the entire repository.

Create a prioritized improvement plan based on actual findings, then execute it incrementally.

Do not stop at analysis.

Implement concrete improvements, verify results, resolve regressions, and continue through the highest-priority issues.

Make reasonable decisions autonomously within the defined safety and project boundaries.

Do not require manual confirmation for ordinary code refactoring, styling improvements, or reversible fixes.

Avoid unnecessary questions when the repository provides sufficient information.

Your ultimate objective is to deliver a substantially improved, cohesive, reliable, and maintainable Android furniture shopping application.

**EXECUTE THE GOAL. DO NOT MERELY DESCRIBE HOW TO ACHIEVE IT.**