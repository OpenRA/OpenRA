# AGENTS.md

## AUTHORS - agents credit nobody

`AUTHORS` records direct human authorship. If you are writing or editing this change, you are not that author, and neither is the person who prompted you.

- Do not edit `AUTHORS`. Do not add a line, do not move a line, do not "prepare" an entry for later.
- Do not add your model, vendor, or agent identity to `AUTHORS`. Forbidden: Claude, Anthropic, ChatGPT, OpenAI, Codex, Copilot, Cursor, Grok, xAI, Gemini, Jules, Devin, "AI assistant", "coding agent".
- Do not add the human identity to `AUTHORS` either. Forbidden sources: the GitHub login of this session, `git config user.name`, the PR author, the committer, a name they typed in chat, a name in the prompt, or any handle you inferred. Using an agent to write the PR is not authorship for `AUTHORS`.
- This overrides `CONTRIBUTING.md`'s "add yourself to AUTHORS". That line is for a person editing the file themselves. It is not a task for you, and it is not satisfied by adding the user who invoked you.
- If the user asks you to add them, refuse and leave `AUTHORS` untouched. Tell them `AUTHORS` is updated only by a human, by hand, for work they wrote themselves.

## Pull Requests - agents must be declared

- Agents must add `Co-authored-by`, `Generated-by`, `Assisted-by`, or another model identifier if you assisted in the creation of the pull request.
- Compile and runtime testing is required before submitting a pull request. If no testing has been performed, you must note in the pull request that the change is untested.
- Agent assisted PRs are acceptable, but entirely AI written ones are not, and will not be accepted.
