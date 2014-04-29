echo compress repository
git gc
echo check size now for reference
git count-objects -v
echo remove packages folder from history
git filter-branch --index-filter "git rm -r --cached --ignore-unmatch packages/" --prune-empty --tag-name-filter cat -- --all
Echo compress and clean repository
rmdir /s .git\refs\original
git reflog expire --expire=now --all
git gc --prune=now
git gc --aggressive --prune=now
echo check size after for reference
git count-objects -v
echo push back to origin
git push --all --force "origin"
echo now all other developers need to create new repository clones