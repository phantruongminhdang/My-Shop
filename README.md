# My Shop Project

PLEASE READ THIS FILE CAREFULLY!

## EF migration

To apply the latest migrations to your physical database, run this command(run the command from `MyProject` folder)

1. Install global tool to make migration(do only 1 time & your machine is good to go for the next)

```
dotnet tool install --global dotnet-ef
```

2. Apply the change

```
dotnet ef database update -s MyProject -p Infrastructures
```

## Branch convention

- For every new function, you must create a new branch, then invite other members to review your code in Git Hub, when that branch merged successfully, you can delete it and continue to create a new branch based on main for other functions.
- When you create a new branch, it must be based on the **main branch** and follow this naming convention:
  **"YourName_FunctionName"**

## Commit naming convention

When you add a new commit, it must follow this naming convention:
**"[YourName][Action description]"**

## Pull request convention

You must create a pull request before your code is merged into main. The pull request must be followed this naming convention:
**"[YourName][YourFunction]"**

## Personal Project

1. Phan Truong Minh Dang
 
