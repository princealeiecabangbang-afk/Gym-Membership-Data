# Gym-Membership-Data
A project that'll give a working Gym Membership database.

The SQL file creates empty tables for members, membership plans, attendance, and payments. It does not include sample records; an application can add and display the data entered by its user.

## Run the application

Install the .NET 10 SDK, then run this command from the repository root:

```bash
dotnet run --project GymManagementSystem/GymManagementSystem.csproj --launch-profile http
```

On first startup, the app initializes `GymManagementSystem/gym.db` from the existing `Database` SQL script. The database file is ignored by Git. EF Core migrations are not used.

The application includes Membership Plans, Members, Attendance, Payments, Dashboard, and Reports pages. Administrator authentication is required for application pages.

## Configure administrator access

There is no default administrator account. Create a password hash in an interactive terminal; the password is hidden while entered:

```bash
dotnet run --project GymManagementSystem/GymManagementSystem.csproj -- --generate-admin-password-hash
```

Copy the printed hash, then configure the username and hash with .NET User Secrets for local development:

Replace the username placeholder with a name you choose and the hash placeholder with the generated hash:

```bash
dotnet user-secrets set "AdminAuthentication:Username" "<your-admin-username>" --project GymManagementSystem/GymManagementSystem.csproj
dotnet user-secrets set "AdminAuthentication:PasswordHash" "<paste-generated-hash>" --project GymManagementSystem/GymManagementSystem.csproj
```

Start the app in Development mode so the User Secrets provider is enabled:

```bash
dotnet run --project GymManagementSystem/GymManagementSystem.csproj --launch-profile http
```

For deployment, set the environment variables `AdminAuthentication__Username` and `AdminAuthentication__PasswordHash` instead of User Secrets. Do not place passwords or hashes in source control or `appsettings.json`. Login POSTs are limited to five per minute per client IP, and the session cookie expires after 30 minutes of inactivity.
