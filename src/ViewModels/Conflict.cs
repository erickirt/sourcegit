﻿using System;

namespace SourceGit.ViewModels
{
    public class ConflictSourceBranch
    {
        public string Name { get; private set; }
        public string Head { get; private set; }
        public Models.Commit Revision { get; private set; }

        public ConflictSourceBranch(string name, string head, Models.Commit revision)
        {
            Name = name;
            Head = head;
            Revision = revision;
        }

        public ConflictSourceBranch(Repository repo, Models.Branch branch)
        {
            Name = branch.Name;
            Head = branch.Head;
            Revision = new Commands.QuerySingleCommit(repo.FullPath, branch.Head).GetResultAsync().Result ?? new Models.Commit() { SHA = branch.Head };
        }
    }

    public class Conflict
    {
        public string Marker
        {
            get => _change.ConflictMarker;
        }

        public string Description
        {
            get => _change.ConflictDesc;
        }

        public object Theirs
        {
            get;
            private set;
        }

        public object Mine
        {
            get;
            private set;
        }

        public bool IsResolved
        {
            get;
            private set;
        } = false;

        public bool CanUseExternalMergeTool
        {
            get;
            private set;
        } = false;

        public Conflict(Repository repo, WorkingCopy wc, Models.Change change)
        {
            _wc = wc;
            _change = change;

            var isSubmodule = repo.Submodules.Find(x => x.Path.Equals(change.Path, StringComparison.Ordinal)) != null;
            if (!isSubmodule && (_change.ConflictReason is Models.ConflictReason.BothAdded or Models.ConflictReason.BothModified))
            {
                CanUseExternalMergeTool = true;
                IsResolved = new Commands.IsConflictResolved(repo.FullPath, change).GetResultAsync().Result;
            }

            switch (wc.InProgressContext)
            {
                case CherryPickInProgress cherryPick:
                    Theirs = cherryPick.Head;
                    Mine = new ConflictSourceBranch(repo, repo.CurrentBranch);
                    break;
                case RebaseInProgress rebase:
                    var b = repo.Branches.Find(x => x.IsLocal && x.Name == rebase.HeadName);
                    if (b != null)
                        Theirs = new ConflictSourceBranch(b.Name, b.Head, rebase.StoppedAt);
                    else
                        Theirs = new ConflictSourceBranch(rebase.HeadName, rebase.StoppedAt?.SHA ?? "----------", rebase.StoppedAt);

                    Mine = rebase.Onto;
                    break;
                case RevertInProgress revert:
                    Theirs = revert.Head;
                    Mine = new ConflictSourceBranch(repo, repo.CurrentBranch);
                    break;
                case MergeInProgress merge:
                    Theirs = merge.Source;
                    Mine = new ConflictSourceBranch(repo, repo.CurrentBranch);
                    break;
                default:
                    Theirs = "Stash or Patch";
                    Mine = new ConflictSourceBranch(repo, repo.CurrentBranch);
                    break;
            }
        }

        public void UseTheirs()
        {
            _wc.UseTheirs([_change]);
        }

        public void UseMine()
        {
            _wc.UseMine([_change]);
        }

        public async void OpenExternalMergeTool()
        {
            await _wc.UseExternalMergeTool(_change);
        }

        private WorkingCopy _wc = null;
        private Models.Change _change = null;
    }
}
