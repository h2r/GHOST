# Contributing Guidelines

This is a research project that many people actively work on. Every single contributor puts in lots of
work to get every minor piece to work, so it's crucial to make sure everyone's code jives well together.

This document describes some basic guidelines for contributing to this project. Most of these guidelines 
are generic and are applicable to every collaborative software project (to varying degrees).

(This document is an adapted version from: https://dzone.com/articles/developer-git-commit-hygiene-1)

## Table of Contents
- [Git Hygiene Best Practices](#git-hygiene-best-practices)
- [Example Workflow](#example-workflow)
- [Git Commit Cheat Sheet](#git-commit-cheat-sheet)

## Git Hygiene Best Practices

Maintaining good commit hygiene is crucial for keeping your Git repository clean, manageable, and 
understandable for everyone involved in the project.

### 0. Basic terminology

The basic contribution unit in Github is the **Pull Request** (or **PR**). Each PR consists of one
or more git **commits** which collectively represent one idea/feature/fix. 

You generally should be able to describe what changes the PR is bringing to the project in a
paragraph or two. This is precisely what you should write in your PR message.

Every PR should be well-tested.

Every PR should be reviewed at least one more person. This is especially crucial if your PR
changes other people's code, or the inputs to other people's code.

A PR essentially _pulls_ all commits from one git **branch** to the other, optionally **squasing**
them first.

### 1. Make Sure You're on the Latest _main_
When you start working on a _new feature_, make sure you're on the latest _main_ branch, and
branch off from there.

**Example:**
```
git checkout main
git pull
git checkout -b my-cool-new-feature
```

If _main_ has moved forward while you're working on your new feature, rebase on the latest main:
```
git checkout main
git pull
git checkout cool-new-feature
git rebase main
<resolve any and all conflicts>
git rebase --continue # Only if you had conflicts
<potentially resolve more conflicts until you're successful>
git status # Make sure everything is clean
```

### 2. Meaningful Commit Messages

#### Write Clear and Descriptive Messages
Each commit message should clearly describe what the commit does:

**Example:**
```
git commit -am "Implement login and logout functionality"
<more work>
git commit -am "Add JWT-based authentication"
<more work>
git commit -am "Add unit tests for authentication logic"
```

### 3. Small, Atomic Commits

#### Commit Small Changes
Make each commit a small, self-contained change that addresses a single issue or feature.
This makes it easier to review and debug.

#### Avoid Large Commits
Large commits are harder to review and understand. Break down large changes into smaller,
logical commits.

### 4. Commit Often, but Not Too Often

#### Frequent Commits
Commit frequently to save your progress and reduce the risk of losing work.

#### Avoid Noise
Don't commit excessively small changes that don't add meaningful value.

### 5. Separate Concerns

#### One Purpose Per Commit
Each commit should have a single purpose. Avoid mixing unrelated changes in a single commit.

**Example:** If you're fixing a bug and refactoring code, do them in separate commits.

**HOWEVER:** This is a research project, and you may find lots of code that could be improved
or modified (e.g. typos, one-liner fixes). You don't need to interrupt your flow to create
a whole separate PR for those types of issues. Use your judgement here.

### 6. Test Before Committing

#### Run Tests
While we don't exactly have unit tests, make sure that the single and multi-spot scenes work 
well before submitting your PR. Make sure to have reasonable coverage of different cases.

### 7. Use Branches Effectively

#### Feature Branches
Develop new features and bug fixes in separate branches rather than directly on the main branch.

#### Branch Naming
Use descriptive branch names (e.g., `feature/add-authentication`).

### 8. Rebase and Squash Commits

#### Rebase Instead of Merge
Use `git rebase` to keep a linear history and avoid unnecessary merge commits.

#### Squash Commits
When your PR is ready to be "merged" make sure to pick the "Squash and merge" or "Rebase and merge".
Use your judgement here (but make sure not to use "Create a merge commit").

### 9. Avoid Committing Generated Files

#### Git Ignore
Use a `.gitignore` file to prevent committing generated files, build artifacts, large dataset files,
or unnecessary files.

**Example:**
```gitignore
# Compiled class files
*.class

# Log files
*.log

# Build directories
/target/
```

### 10. Document Important Changes

#### PR Message Body
Provide additional context in the PR message body if the change is complex or requires explanation.

**Example:**
```
Refactor authentication module
- Simplify token validation logic
- Improve error handling in login process
- Update documentation for the new authentication flow
```

### 11. Review Commits Before Pushing

#### Interactive Rebase
Use interactive rebase (`git rebase -i`) to review and clean up your commits before pushing.

#### Amend Last Commit
If you need to make small changes to the last commit, use `git commit --amend`.

---

## Example Workflow

### Step-by-Step Process

1. **Stage Changes**
   ```bash
   git add <new file>
   ```

2. **Commit With a Descriptive Message**
   ```bash
   git commit -am "Add user authentication module"
   ```

3. **Review Commits**
   ```bash
   git log
   ```

4. **Rebase and Squash Commits if Necessary**
   ```bash
   git rebase -i HEAD~N
   ```

5. **Push To Remote Repository**
   ```bash
   git push origin feature/add-authentication
   ```

---

## Git Commit Cheat Sheet

> **Source:** GitLab

### Starting a Project

| Command | Description |
|---------|-------------|
| `git init [project name]` | Create a new local repository in the current directory |
| `git clone <Project URL>` | Downloads a project with the entire history from the remote repository |

### Git Configuration

Run these when you're inside your repository. You may use `--global` if you're running on your own machine.
| Command | Description |
|---------|-------------|
| `git config --local user.name "Your Name"` | Set the name that will be attached to your commits and tags |
| `git config --local user.email "you@example.com"` | Set the e-mail address that will be attached to your commits and tags |
| `git config --local color.ui auto` | Enable some colorization of Git output |

### Day-To-Day Work

| Command | Description |
|---------|-------------|
| `git status` | Displays the status of your working directory |
| `git add [file]` | Add a file to the staging area |
| `git diff [file]` | Show changes between working directory and staging area |
| `git diff --staged [file]` | Shows any changes between the staging area and the repository |
| `git checkout -- [file]` | Discard changes in working directory ⚠️ *This operation is unrecoverable* |
| `git reset [path...]` | Revert some paths in the index to their state in HEAD |
| `git commit` | Create a new commit from changes added to the staging area |
| `git rm [file]` | Remove file from working directory and staging area |

### Storing Your Work

| Command | Description |
|---------|-------------|
| `git stash` | Put current changes in your working directory into stash for later use |
| `git stash pop` | Apply stored stash content into working directory, and clear stash |
| `git stash drop` | Delete a specific stash from all your previous stashes |

### Git Branching Model

| Command | Description |
|---------|-------------|
| `git branch [-a]` | List all local branches in repository. With `-a`: show all branches (with remote) |
| `git branch [branch_name]` | Create new branch, referencing the current HEAD |
| `git rebase [branch_name]` | Apply commits of the current working branch to the HEAD of [branch] |
| `git checkout [-b] [branch_name]` | Switch working directory to the specified branch. With `-b`: create if doesn't exist |
| `git branch -d [branch_name]` | Remove selected branch, if it is already merged. `-D` forces deletion |

### Tagging Commits

| Command | Description |
|---------|-------------|
| `git tag` | List all tags |
| `git tag [name] [commit sha]` | Create a tag reference named name for current commit |
| `git tag -a [name] [commit sha]` | Create a tag object named name for current commit |
| `git tag -d [name]` | Remove a tag from local repository |

### Synchronizing Repositories

`[remote]` is generally just `origin`
| Command | Description |
|---------|-------------|
| `git fetch [remote]` | Fetch changes from the remote, but not update tracking branches |
| `git fetch --prune [remote]` | Delete remote refs that were removed from the remote repository |
| `git pull [remote]` | Fetch changes from the remote and merge current branch with its upstream |
| `git push [--tags] [remote]` | Push local changes to the remote. Use `--tags` to push tags |
| `git push -u [remote] [branch]` | Push local branch to remote repository. Set its copy as an upstream |

### Inspect History

| Command | Description |
|---------|-------------|
| `git log [-n count]` | List commit history of current branch. `-n count` limits list to last n commits |
| `git log --oneline --graph --decorate` | An overview with reference labels and history graph |
| `git log ref..` | List commits that are present on the current branch and not merged into ref |
| `git blame [filename]` | Displays a file line-by-line with the corresponding commit that last modified each line |

### Reverting Changes

| Command | Description |
|---------|-------------|
| `git reset [--hard] [target reference]` | Switches the current branch to the target reference ⚠️ *Use `--hard` carefully* |
| `git revert [commit sha]` | Create a new commit, reverting changes from the specified commit |

---

## Final Thoughts

By following these practices, you can ensure good commit hygiene, making your Git history more readable and maintainable for you and everyone on the team. Remember:

- **Quality over quantity** - Make meaningful commits
- **Consistency is key** - Keeps the project running for a long time
- **Documentation matters** - Clear commit messages save time
- **Test before committing** - Prevent broken builds
- **Review your work** - Clean up before pushing

---
