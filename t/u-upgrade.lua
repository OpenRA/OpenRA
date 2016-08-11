local ver311 = ide.wxver >= "3.1.1"
ok(ver311 and wx.wxFileName().ShouldFollowLink or nil, "wxwidgets 3.1.1 includes wxFileName().ShouldFollowLink")
