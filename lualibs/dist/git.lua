-- Encapsulated Git functionality

module ("dist.git", package.seeall)

require "git"
local sys = require "dist.sys"
local cfg = require "dist.config"


-- Clone the repository from url to dest_dir
function clone(repository_url, dest_dir, depth, branch)
    assert(type(repository_url) == "string", "git.clone: Argument 'repository_url' is not a string.")
    assert(type(dest_dir) == "string", "git.clone: Argument 'dest_dir' is not a string.")
    dest_dir = sys.abs_path(dest_dir)

    local command = "git clone " .. repository_url

    if depth then
        assert(type(depth) == "number", "git.clone: Argument 'depth' is not a number.")
        command = command .. " --depth " .. depth
    end

    if branch then
        assert(type(branch) == "string", "git.clone: Argument 'branch' is not a string.")
        command = command .. " -b " .. branch
    end

    command = command .. " " .. sys.quote(dest_dir)
    if sys.exists(dest_dir) then sys.delete(dest_dir) end
    sys.make_dir(dest_dir)

    -- change the current working directory to dest_dir
    local prev_current_dir = sys.current_dir()
    sys.change_dir(dest_dir)

    -- execute git clone
    if not cfg.debug then command = command .. " -q " end
    local ok, err = sys.exec(command)

    -- change the current working directory back
    sys.change_dir(prev_current_dir)

    return ok, err
end

-- Return table of all refs of the remote repository at the 'git_url'. Ref_type can be "tags" or "heads".
local function get_remote_refs(git_url, ref_type)
    assert(type(git_url) == "string", "git.get_remote_refs: Argument 'git_url' is not a string.")
    assert(type(ref_type) == "string", "git.get_remote_refs: Argument 'ref_type' is not a string.")
    assert(ref_type == "tags" or ref_type == "heads", "git.get_remote_refs: Argument 'ref_type' is not \"tags\" or \"heads\".")

    local refs = {}

    local ok, refs_or_err = pcall(git.protocol.remotes, git_url)
    if not ok then return nil, "Error getting refs of the remote repository '" .. git_url .. "': " .. refs_or_err end

    for ref, sha in pairs(refs_or_err) do
        if ref:match("%S+/" .. ref_type .. "/%S+") and not ref:match("%^{}") then
            table.insert(refs, ref:match("%S+/" .. ref_type .. "/(%S+)"))
        end
    end

    return refs
end

-- Return table of all tags of the repository at the 'git_url'
function get_remote_tags(git_url)
    return get_remote_refs(git_url, "tags")
end

-- Return table of all branches of the repository at the 'git_url'
function get_remote_branches(git_url)
    return get_remote_refs(git_url, "heads")
end

-- Checkout specified ref in specified git_repo_dir
function checkout_ref(ref, git_repo_dir, orphaned)
    git_repo_dir = git_repo_dir or sys.current_dir()
    orphaned = orphaned or false
    assert(type(ref) == "string", "git.checkout_ref: Argument 'ref' is not a string.")
    assert(type(git_repo_dir) == "string", "git.checkout_ref: Argument 'git_repo_dir' is not a string.")
    assert(type(orphaned) == "boolean", "git.checkout_ref: Argument 'orphaned' is not a boolean.")
    git_repo_dir = sys.abs_path(git_repo_dir)

    local command = "git checkout "
    if orphaned then command = command .. " --orphan " end
    command = command .. " " .. ref .. " -f"
    if not cfg.debug then command = command .. " -q " end

    local ok, err
    if git_repo_dir ~= sys.current_dir() then
        local prev_current_dir = sys.current_dir()
        sys.change_dir(git_repo_dir)
        ok, err = sys.exec(command)
        sys.change_dir(prev_current_dir)
    else
        ok, err = sys.exec(command)
    end

    return ok, err
end

-- Checkout specified sha in specified git_repo_dir
function checkout_sha(sha, git_repo_dir)
    git_repo_dir = git_repo_dir or sys.current_dir()
    assert(type(sha) == "string", "git.checkout_sha: Argument 'sha' is not a string.")
    assert(type(git_repo_dir) == "string", "git.checkout_sha: Argument 'git_repo_dir' is not a string.")
    git_repo_dir = sys.abs_path(git_repo_dir)

    local dir_changed, prev_current_dir

    if git_repo_dir ~= sys.current_dir() then
        prev_current_dir = sys.current_dir()
        sys.change_dir(git_repo_dir)
        dir_changed = true
    end

    local ok, repo_or_err = pcall(git.repo.open, git_repo_dir)
    if not ok then return nil, "Error when opening the git repository '" .. git_repo_dir .. "': " .. repo_or_err end

    local err
    ok, err = pcall(repo_or_err.checkout, repo_or_err, sha, git_repo_dir)
    if not ok then return nil, "Error when checking out the sha '" .. sha .. "' in the git repository '" .. git_repo_dir .. "': " .. err end

    repo_or_err:close()
    if dir_changed then sys.change_dir(prev_current_dir) end

    return true
end

-- Create an empty git repository in given directory.
function init(dir)
    dir = dir or sys.current_dir()
    assert(type(dir) == "string", "git.init: Argument 'dir' is not a string.")
    dir = sys.abs_path(dir)

    -- create the 'dir' first, since it causes 'git init' to fail on Windows
    -- when the parent directory of 'dir' doesn't exist
    local ok, err = sys.make_dir(dir)
    if not ok then return nil, err end

    local command = "git init " .. sys.quote(dir)
    if not cfg.debug then command = command .. " -q " end
    return sys.exec(command)
end

-- Add all files in the 'repo_dir' to the git index. The 'repo_dir' must be
-- in the initialized git repository.
function add_all(repo_dir)
    repo_dir = repo_dir or sys.current_dir()
    assert(type(repo_dir) == "string", "git.add_all: Argument 'repo_dir' is not a string.")
    repo_dir = sys.abs_path(repo_dir)

    local ok, prev_dir, msg
    ok, prev_dir = sys.change_dir(repo_dir);
    if not ok then return nil, err end

    ok, msg = sys.exec("git add -A -f " .. sys.quote(repo_dir))
    sys.change_dir(prev_dir)

    return ok, msg
end

-- Commit all indexed files in 'repo_dir' with the given commit 'message'.
-- The 'repo_dir' must be in the initialized git repository.
function commit(message, repo_dir)
    repo_dir = repo_dir or sys.current_dir()
    message = message or "commit by luadist-git"
    assert(type(message) == "string", "git.commit: Argument 'message' is not a string.")
    assert(type(repo_dir) == "string", "git.commit: Argument 'repo_dir' is not a string.")
    repo_dir = sys.abs_path(repo_dir)

    local ok, prev_dir, msg
    ok, prev_dir = sys.change_dir(repo_dir);
    if not ok then return nil, err end

    local command = "git commit -m " .. sys.quote(message)
    if not cfg.debug then command = command .. " -q " end
    ok, msg = sys.exec(command)
    sys.change_dir(prev_dir)

    return ok, msg
end


-- Rename branch 'old_name' to 'new_name'. -- The 'repo_dir' must be
-- in the initialized git repository and the branch 'new_name' must
-- not already exist in that repository.
function rename_branch(old_name, new_name, repo_dir)
    repo_dir = repo_dir or sys.current_dir()
    assert(type(old_name) == "string", "git.rename_branch: Argument 'old_name' is not a string.")
    assert(type(new_name) == "string", "git.rename_branch: Argument 'new_name' is not a string.")
    assert(type(repo_dir) == "string", "git.rename_branch: Argument 'repo_dir' is not a string.")
    repo_dir = sys.abs_path(repo_dir)

    local ok, prev_dir, msg
    ok, prev_dir = sys.change_dir(repo_dir);
    if not ok then return nil, err end

    ok, msg = sys.exec("git branch -m " .. old_name .. " " .. new_name)
    sys.change_dir(prev_dir)

    return ok, msg
end

-- Push the ref 'ref_name' from the 'repo_dir' to the remote git
-- repository 'git_repo_url'. If 'all_tags' is set to true, all tags
-- will be pushed, in addition to the explicitly given ref.
-- If 'delete' is set to 'true' then the explicitly given remote ref
-- will be deleted, not pushed.
function push_ref(repo_dir, ref_name, git_repo_url, all_tags, delete)
    repo_dir = repo_dir or sys.current_dir()
    all_tags = all_tags or false
    delete = delete or false
    assert(type(repo_dir) == "string", "git.push_ref: Argument 'repo_dir' is not a string.")
    assert(type(git_repo_url) == "string", "git.push_ref: Argument 'git_repo_url' is not a string.")
    assert(type(ref_name) == "string", "git.push_ref: Argument 'ref_name' is not a string.")
    assert(type(all_tags) == "boolean", "git.push_ref: Argument 'all_tags' is not a boolean.")
    assert(type(delete) == "boolean", "git.push_ref: Argument 'delete' is not a boolean.")
    repo_dir = sys.abs_path(repo_dir)

    local ok, prev_dir, msg
    ok, prev_dir = sys.change_dir(repo_dir);
    if not ok then return nil, err end

    local command = "git push " .. git_repo_url
    if all_tags then command = command .. " --tags " end
    if delete then command = command .. " --delete " end
    command = command .. " " .. ref_name .. " -f "
    if not cfg.debug then command = command .. " -q " end

    ok, msg = sys.exec(command)
    sys.change_dir(prev_dir)

    return ok, msg
end

-- Creates the tag 'tag_name' in given 'repo_dir', which must be
-- in the initialized git repository
function create_tag(repo_dir, tag_name)
    repo_dir = repo_dir or sys.current_dir()
    assert(type(repo_dir) == "string", "git.create_tag: Argument 'repo_dir' is not a string.")
    assert(type(tag_name) == "string", "git.create_tag: Argument 'tag_name' is not a string.")
    repo_dir = sys.abs_path(repo_dir)

    local ok, prev_dir, msg
    ok, prev_dir = sys.change_dir(repo_dir);
    if not ok then return nil, err end

    ok, msg = sys.exec("git tag " .. tag_name .. " -f ")
    sys.change_dir(prev_dir)

    return ok, msg
end

-- Fetch given 'ref_name' from the remote 'git_repo_url' to the local repository
-- 'repo_dir' and return its sha. 'ref_type' can be "tag" or "head".
local function fetch_ref(repo_dir, git_repo_url, ref_name, ref_type)
    repo_dir = repo_dir or sys.current_dir()
    assert(type(repo_dir) == "string", "git.fetch_ref: Argument 'repo_dir' is not a string.")
    assert(type(git_repo_url) == "string", "git.fetch_ref: Argument 'git_repo_url' is not a string.")
    assert(type(ref_name) == "string", "git.fetch_ref: Argument 'ref_name' is not a string.")
    assert(type(ref_type) == "string", "git.fetch_ref: Argument 'ref_type' is not a string.")
    assert(ref_type == "tag" or ref_type == "head", "git.get_remote_refs: Argument 'ref_type' is not \"tag\" or \"head\".")
    repo_dir = sys.abs_path(repo_dir)

    local refstring = "refs/" .. ref_type .. "s/" .. ref_name

    local suppress_fetch_progress = not cfg.debug
    local ok, repo_or_err = pcall(git.repo.open, repo_dir)
    if not ok then return nil, "Error when opening the git repository '" .. repo_dir .. "': " .. repo_or_err end

    local ok, pack_or_err, sha = pcall(git.protocol.fetch, git_repo_url, repo_or_err, refstring, suppress_fetch_progress)
    if not ok then return nil, "Error when fetching ref '" .. refstring .. "' from git repository '" .. git_repo_url .. "': " .. pack_or_err end

    repo_or_err:close()
    pack_or_err:close()

    return sha
end

-- Fetch given 'tag_name' from the remote 'git_repo_url' to the local repository
-- 'repo_dir' and save it as a tag with the same 'tag_name'.
function fetch_tag(repo_dir, git_repo_url, tag_name)
    return fetch_ref(repo_dir, git_repo_url, tag_name, "tag")
end

-- Fetch given 'branch_name' from the remote 'git_repo_url' to the local repository
-- 'repo_dir' and save it as a branch with the same 'branch_name'.
function fetch_branch(repo_dir, git_repo_url, branch_name)
    return fetch_ref(repo_dir, git_repo_url, branch_name, "head")
end

-- Create the git repository and return the repo object (which can be used in checkout_sha etc.)
-- If the 'dir' exists, it's deleted prior to creating the git repository.
function create_repo(dir)
    assert(type(dir) == "string", "git.create_repo: Argument 'dir' is not a string.")

    if sys.exists(dir) then sys.delete(dir) end

    local ok, repo_or_err = pcall(git.repo.create, dir)
    if not ok then return nil, "Error when creating the git repository '" .. dir .. "': " .. repo_or_err end

    repo_or_err:close()
    return true
end
