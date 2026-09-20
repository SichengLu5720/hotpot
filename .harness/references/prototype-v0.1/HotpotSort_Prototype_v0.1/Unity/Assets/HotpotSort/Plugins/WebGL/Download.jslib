mergeInto(LibraryManager.library, {
  HotpotDownload: function(filename, contents) {
    var name=UTF8ToString(filename), text=UTF8ToString(contents);
    var url=URL.createObjectURL(new Blob([text],{type:'application/json'}));
    var a=document.createElement('a'); a.href=url; a.download=name; a.click();
    setTimeout(function(){URL.revokeObjectURL(url);},2000);
  }
});
