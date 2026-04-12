# Changelog

## [2026-04-12]
### Added
- MongoDB startup connectivity validation via `MongoDbInitializer` without requiring initialization scripts.
- MongoDB health check registration for `DatabaseProvider.MongoDb` policies.
- Core caching integration via `ApplicationConfiguration.CachingPolicy` and `AddAbsoluteAlgorithmCaching`.

### Changed
- Upgraded `AbsoluteAlgorithm.Core` dependency to `1.0.1-beta.14`.
- Added caching service registration flow in infrastructure bootstrap.

### Fixed
- Skipped Dapper `Repository` keyed registration for `DatabaseProvider.MongoDb` to prevent runtime mismatches.
- Standardized provider usage across infrastructure to use `DatabaseProvider`.

## [2026-03-21]
### Tested
- Authentication and storage controllers with corresponding tests.
- Implemented JWT, cookie, and API key authentication in AuthController.
- Created StorageController for file uploads and downloads to MinIO, Azure, and S3.
- Tested versioning support in VersionController with multiple API versions.
- Implemented WebhookController for handling webhook callbacks and signature verification.
- Created HTTP request files for testing authentication, storage, versioning, and webhooks.
- Added sample text file for storage tests.

## [2026-03-20]
### Changed
- Various internal changes and refactoring.

## [2026-03-19]
### Added
- Icon for NuGet package.
- Metadata view and library code hiding for NuGet.
- AppConfiguration validation.
- Idempotency support.
- Authentication helpers.
- Pagination, filtering, and sorting.
- Improved health check model.
- Webhook/request-signature validation.
- ETag/Optimistic Concurrency helpers.
- AuthorizeKey attribute.
- Swagger and documentation improvements.
- Polly-based HTTP and database resilience.
- CSRF protection.
- Utility classes for hashing, encryption, tokens, ETags, files, JSON, claims, compression, reflection, enums, HTTP helpers, and more.

## [2026-03-16]
### Fixed
- nlog.settings.json inclusion in NuGet package.

## [2026-03-15]
### Added
- Storage service abstraction and providers (S3, Azure Blob, MinIO, GCP).
- Health checks for storage and database.
- README and documentation.
- Initial project setup and solution structure.

---