# TrustVault Security Threats and Tests

## 1. Unauthorized Document Access

- **Threat:** A logged-in user attempts to access a document belonging to another user without permission.
- **Mitigation:** The backend verifies document ownership and active permissions before allowing document access.
- **Test:** Log in as a user without permission and attempt to download another user's document.
- **Observed result:** During testing, a user without access was unable to download a document after access was revoked. Additional cross-user access tests should also be performed.

## 2. Revoked Permissions

- **Threat:** A recipient attempts to download a document after the owner revokes access.
- **Mitigation:** The backend checks the current permission status for each download request.
- **Test:** Share a document with another user, revoke access, and try downloading it from the recipient's account.
- **Observed result:** The revoked user's document request returned HTTP 404 during testing.

## 3. Invalid Authentication

- **Threat:** An unauthenticated user attempts to access protected API endpoints.
- **Mitigation:** JWT authentication protects endpoints that require a signed-in user.
- **Test:** Request a protected endpoint without a valid JWT.
- **Expected result:** The request is rejected. Confirm the exact HTTP status during testing.

## 4. Exposure of Confidential Files

- **Threat:** Uploaded documents become publicly accessible through storage.
- **Mitigation:** Documents are stored in the private Supabase Storage bucket `trustvault-files`. The backend mediates document access.
- **Test:** Verify that the bucket is private and test file access through the application's authorization checks.
- **Observed result:** The Supabase dashboard showed the Public bucket setting turned off and a 10 MB file size limit enabled. Application-level access checks must also be tested to confirm that unauthorized users cannot retrieve files.

## 5. Missing Audit Records

- **Threat:** Document activity occurs without a corresponding audit entry.
- **Mitigation:** The application records activities such as document upload, download, sharing, and permission revocation.
- **Test:** Perform each action and inspect the audit trail for its corresponding entry.
- **Status:** Verify the audit entry for each action during final testing. Do not mark an action as verified until its entry has been checked.

## 6. Secret Exposure

- **Threat:** Passwords, JWT signing keys, or storage credentials are committed to the public repository.
- **Mitigation:** Store secrets in environment variables or .NET User Secrets. Do not commit credentials to source control.
- **Test:** Review tracked source files and repository history for exposed credentials. Rotate any credential that may have been exposed.
- **Status:** Review the repository and its history before claiming this check is complete.

## Limitations

These threats and tests cover important security risks but do not prove that every possible attack is prevented. Additional automated security tests, input validation checks, and edge-case testing are recommended. Results should be reported honestly based on tests actually performed.
