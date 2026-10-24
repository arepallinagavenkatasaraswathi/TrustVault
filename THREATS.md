# TrustVault Security Threats and Tests

## 1. Unauthorized document access
- **Threat:** A logged-in user attempts to access a document that belongs to another user.
- **Mitigation:** The backend checks document ownership and active permissions before allowing access.
- **Test:** Log in as a user without permission and attempt to download another user's document.
- **Expected result:** Access is denied.

## 2. Revoked permissions
- **Threat:** A user tries to download a document after the owner revokes access.
- **Mitigation:** The backend checks current permission status for each download.
- **Test:** Share a document, revoke access, and try to download it using the recipient's account.
- **Observed result:** The revoked user's document request returned 404 during testing.

## 3. Invalid authentication
- **Threat:** An unauthenticated user attempts to access protected API endpoints.
- **Mitigation:** JWT authentication protects endpoints that require a signed-in user.
- **Test:** Request a protected endpoint without a valid token.
- **Expected result:** The request is rejected.

## 4. Exposure of confidential files
- **Threat:** Uploaded documents become publicly accessible through storage.
- **Mitigation:** Uploaded files are stored in a private Supabase Storage bucket, and access is mediated by the backend.
- **Test:** Verify the bucket is private and test file access through the application's authorization checks.
- **Status:** Confirm the storage access configuration before claiming this test is complete.

## 5. Missing audit records
- **Threat:** Document sharing or access activity occurs without a corresponding audit entry.
- **Mitigation:** The application records document activities such as upload, download, share, and revoke.
- **Test:** Perform these actions and inspect the audit trail for corresponding entries.
- **Status:** Verify each action's audit entry during final testing.

## 6. Secret exposure
- **Threat:** Passwords, JWT signing keys, or storage credentials are committed to the public repository.
- **Mitigation:** Keep secrets in environment variables or .NET User Secrets, and never commit them to source control.
- **Test:** Review tracked source files and repository history for exposed secrets. Rotate any credential that was exposed.

## Limitations
This document describes identified risks and tests. It does not claim that all possible attacks have been prevented. Additional automated security tests and edge-case testing are still recommended.
