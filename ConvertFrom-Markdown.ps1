<#
.SYNOPSIS
   Converts Markdown formatted text to HTML.
.DESCRIPTION
   Converts Markdown formatted text to HTML using the Github API. Output is "flavored" depending on
   the chosen mode. The default output flavor is 'Markdown' and includes Syntax highlighting and
   Github stylesheets.

   Based on the Ruby version by Brett Terpstra:
   http://brettterpstra.com/easy-command-line-github-flavored-markdown/

   About Markdown: http://daringfireball.net/projects/markdown/
#>
function ConvertFrom-Markdown {
    [CmdletBinding()]
    Param
    (
        [Parameter(Mandatory=$true, ValueFromPipeline=$true, Position=0)]
        [PSObject[]]$InputObject
    )

    Begin
    {
        $URL = "https://api.github.com/markdown"
    }

    Process
    {
        Foreach ($item in $InputObject)
        {
            $object = New-Object -TypeName psobject
            $object | Add-Member -MemberType NoteProperty -Name 'text' -Value ($item | Out-String)
            $object | Add-Member -MemberType NoteProperty -Name 'mode' -Value 'markdown'

            $response = Invoke-WebRequest -Method Post -Uri $url -Body ($object | ConvertTo-Json)

            if ($response.StatusCode -eq "200")
            {
                $HtmlOutput =
                @"
                <!DOCTYPE HTML>
                <html lang="en-US">
                    <head>
	                    <meta charset="UTF-8">
	                    <style>
	                        html,body{color:black}*{margin:0;padding:0}body{font:13.34px helvetica,arial,freesans,clean,sans-serif;-webkit-font-smoothing:subpixel-antialiased;line-height:1.4;padding:3px;background:#fff;border-radius:3px;-moz-border-radius:3px;-webkit-border-radius:3px}p{margin:1em 0}a{color:#4183c4;text-decoration:none}#wrapper{background-color:#fff;padding:30px;font-size:14px;line-height:1.6;width:900px;margin:15px auto;}@media screen{#wrapper{box-shadow:0 0 0 1px #cacaca,0 0 0 4px #eee}}#wrapper>*:first-child{margin-top:0!important}#wrapper>*:last-child{margin-bottom:0!important}h1,h2,h3,h4,h5,h6{margin:20px 0 10px;padding:0;font-weight:bold;-webkit-font-smoothing:subpixel-antialiased;cursor:text}h1{font-size:28px;color:#000}h2{font-size:24px;border-bottom:1px solid #ccc;color:#000}h3{font-size:18px;color:#333}h4{font-size:16px;color:#333}h5{font-size:14px;color:#333}h6{color:#777;font-size:14px}p,blockquote,ul,ol,dl,table,pre{margin:15px 0}ul,ol{padding-left:30px}hr{background:transparent url(data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAYAAAAECAYAAACtBE5DAAAAGXRFWHRTb2Z0d2FyZQBBZG9iZSBJbWFnZVJlYWR5ccllPAAAAyJpVFh0WE1MOmNvbS5hZG9iZS54bXAAAAAAADw/eHBhY2tldCBiZWdpbj0i77u/IiBpZD0iVzVNME1wQ2VoaUh6cmVTek5UY3prYzlkIj8+IDx4OnhtcG1ldGEgeG1sbnM6eD0iYWRvYmU6bnM6bWV0YS8iIHg6eG1wdGs9IkFkb2JlIFhNUCBDb3JlIDUuMC1jMDYwIDYxLjEzNDc3NywgMjAxMC8wMi8xMi0xNzozMjowMCAgICAgICAgIj4gPHJkZjpSREYgeG1sbnM6cmRmPSJodHRwOi8vd3d3LnczLm9yZy8xOTk5LzAyLzIyLXJkZi1zeW50YXgtbnMjIj4gPHJkZjpEZXNjcmlwdGlvbiByZGY6YWJvdXQ9IiIgeG1sbnM6eG1wPSJodHRwOi8vbnMuYWRvYmUuY29tL3hhcC8xLjAvIiB4bWxuczp4bXBNTT0iaHR0cDovL25zLmFkb2JlLmNvbS94YXAvMS4wL21tLyIgeG1sbnM6c3RSZWY9Imh0dHA6Ly9ucy5hZG9iZS5jb20veGFwLzEuMC9zVHlwZS9SZXNvdXJjZVJlZiMiIHhtcDpDcmVhdG9yVG9vbD0iQWRvYmUgUGhvdG9zaG9wIENTNSBNYWNpbnRvc2giIHhtcE1NOkluc3RhbmNlSUQ9InhtcC5paWQ6OENDRjNBN0E2NTZBMTFFMEI3QjRBODM4NzJDMjlGNDgiIHhtcE1NOkRvY3VtZW50SUQ9InhtcC5kaWQ6OENDRjNBN0I2NTZBMTFFMEI3QjRBODM4NzJDMjlGNDgiPiA8eG1wTU06RGVyaXZlZEZyb20gc3RSZWY6aW5zdGFuY2VJRD0ieG1wLmlpZDo4Q0NGM0E3ODY1NkExMUUwQjdCNEE4Mzg3MkMyOUY0OCIgc3RSZWY6ZG9jdW1lbnRJRD0ieG1wLmRpZDo4Q0NGM0E3OTY1NkExMUUwQjdCNEE4Mzg3MkMyOUY0OCIvPiA8L3JkZjpEZXNjcmlwdGlvbj4gPC9yZGY6UkRGPiA8L3g6eG1wbWV0YT4gPD94cGFja2V0IGVuZD0iciI/PqqezsUAAAAfSURBVHjaYmRABcYwBiM2QSA4y4hNEKYDQxAEAAIMAHNGAzhkPOlYAAAAAElFTkSuQmCC) repeat-x 0 0;border:0 none;color:#ccc;height:4px;padding:0}#wrapper>h2:first-child,#wrapper>h1:first-child,#wrapper>h1:first-child+h2,#wrapper>h3:first-child,#wrapper>h4:first-child,#wrapper>h5:first-child,#wrapper>h6:first-child{margin-top:0;padding-top:0}a:first-child h1,a:first-child h2,a:first-child h3,a:first-child h4,a:first-child h5,a:first-child h6{margin-top:0;padding-top:0}h1+p,h2+p,h3+p,h4+p,h5+p,h6+p{margin-top:0}ul li>:first-child,ol li>:first-child{margin-top:0}dl{padding:0}dl dt{font-size:14px;font-weight:bold;font-style:italic;padding:0;margin:15px 0 5px}dl dt:first-child{padding:0}dl dt>:first-child{margin-top:0}dl dt>:last-child{margin-bottom:0}dl dd{margin:0 0 15px;padding:0 15px}dl dd>:first-child{margin-top:0}dl dd>:last-child{margin-bottom:0}blockquote{border-left:4px solid #DDD;padding:0 15px;color:#777}blockquote>:first-child{margin-top:0}blockquote>:last-child{margin-bottom:0}table{border-collapse:collapse;border-spacing:0;font-size:100%;font:inherit}table th{font-weight:bold}table th,table td{border:1px solid #ccc;padding:6px 13px}table tr{border-top:1px solid #ccc;background-color:#fff}table tr:nth-child(2n){background-color:#f8f8f8}img{max-width:100%}code,tt{margin:0 2px;padding:0 5px;white-space:nowrap;border:1px solid #eaeaea;background-color:#f8f8f8;border-radius:3px}pre>code{margin:0;padding:0;white-space:pre;border:0;background:transparent}.highlight pre,pre{background-color:#f8f8f8;border:1px solid #ccc;font-size:13px;line-height:19px;overflow:auto;padding:6px 10px;border-radius:3px}pre code,pre tt{background-color:transparent;border:0}.poetry pre{font-family:Georgia,Garamond,serif!important;font-style:italic;font-size:110%!important;line-height:1.6em;display:block;margin-left:1em}.poetry pre code{font-family:Georgia,Garamond,serif!important;word-break:break-all;word-break:break-word;-webkit-hyphens:auto;-moz-hyphens:auto;hyphens:auto;white-space:pre-wrap}sup,sub,a.footnote{font-size:1.4ex;height:0;line-height:1;vertical-align:super;position:relative}sub{vertical-align:sub;top:-1px}@media print{body{background:#fff}img,pre,blockquote,table,figure{page-break-inside:avoid}#wrapper{background:#fff;border:0}code{background-color:#fff;color:#444!important;padding:0 .2em;border:1px solid #dedede}pre code{background-color:#fff!important;overflow:visible}pre{background:#fff}}@media screen{body.inverted,.inverted #wrapper,.inverted hr .inverted p,.inverted td,.inverted li,.inverted h1,.inverted h2,.inverted h3,.inverted h4,.inverted h5,.inverted h6,.inverted th,.inverted .math,.inverted caption,.inverted dd,.inverted dt,.inverted blockquote{color:#eee!important;border-color:#555}.inverted td,.inverted th{background:#333}.inverted pre,.inverted code,.inverted tt{background:#444!important}.inverted h2{border-color:#555}.inverted hr{border-color:#777;border-width:1px!important}::selection{background:rgba(157,193,200,.5)}h1::selection{background-color:rgba(45,156,208,.3)}h2::selection{background-color:rgba(90,182,224,.3)}h3::selection,h4::selection,h5::selection,h6::selection,li::selection,ol::selection{background-color:rgba(133,201,232,.3)}code::selection{background-color:rgba(0,0,0,.7);color:#eee}code span::selection{background-color:rgba(0,0,0,.7)!important;color:#eee!important}a::selection{background-color:rgba(255,230,102,.2)}.inverted a::selection{background-color:rgba(255,230,102,.6)}td::selection,th::selection,caption::selection{background-color:rgba(180,237,95,.5)}.inverted{background:#0b2531}.inverted #wrapper,.inverted{background:rgba(37,42,42,1)}.inverted a{color:rgba(172,209,213,1)}}.highlight .c{color:#998;font-style:italic}.highlight .err{color:#a61717;background-color:#e3d2d2}.highlight .k{font-weight:bold}.highlight .o{font-weight:bold}.highlight .cm{color:#998;font-style:italic}.highlight .cp{color:#999;font-weight:bold}.highlight .c1{color:#998;font-style:italic}.highlight .cs{color:#999;font-weight:bold;font-style:italic}.highlight .gd{color:#000;background-color:#fdd}.highlight .gd .x{color:#000;background-color:#faa}.highlight .ge{font-style:italic}.highlight .gr{color:#a00}.highlight .gh{color:#999}.highlight .gi{color:#000;background-color:#dfd}.highlight .gi .x{color:#000;background-color:#afa}.highlight .go{color:#888}.highlight .gp{color:#555}.highlight .gs{font-weight:bold}.highlight .gu{color:#800080;font-weight:bold}.highlight .gt{color:#a00}.highlight .kc{font-weight:bold}.highlight .kd{font-weight:bold}.highlight .kn{font-weight:bold}.highlight .kp{font-weight:bold}.highlight .kr{font-weight:bold}.highlight .kt{color:#458;font-weight:bold}.highlight .m{color:#099}.highlight .s{color:#d14}.highlight .na{color:#008080}.highlight .nb{color:#0086b3}.highlight .nc{color:#458;font-weight:bold}.highlight .no{color:#008080}.highlight .ni{color:#800080}.highlight .ne{color:#900;font-weight:bold}.highlight .nf{color:#900;font-weight:bold}.highlight .nn{color:#555}.highlight .nt{color:#000080}.highlight .nv{color:#008080}.highlight .ow{font-weight:bold}.highlight .w{color:#bbb}.highlight .mf{color:#099}.highlight .mh{color:#099}.highlight .mi{color:#099}.highlight .mo{color:#099}.highlight .sb{color:#d14}.highlight .sc{color:#d14}.highlight .sd{color:#d14}.highlight .s2{color:#d14}.highlight .se{color:#d14}.highlight .sh{color:#d14}.highlight .si{color:#d14}.highlight .sx{color:#d14}.highlight .sr{color:#009926}.highlight .s1{color:#d14}.highlight .ss{color:#990073}.highlight .bp{color:#999}.highlight .vc{color:#008080}.highlight .vg{color:#008080}.highlight .vi{color:#008080}.highlight .il{color:#099}.highlight .gc{color:#999;background-color:#eaf2f5}.type-csharp .highlight .k{color:#00F}.type-csharp .highlight .kt{color:#00F}.type-csharp .highlight .nf{color:#000;font-weight:normal}.type-csharp .highlight .nc{color:#2b91af}.type-csharp .highlight .nn{color:#000}.type-csharp .highlight .s{color:#a31515}.type-csharp .highlight .sc{color:#a31515}
	                    </style>
                    </head>
                    <body>
                        <div id="wrapper">
                           $($response.Content)
                        </div>
                    </body>
                </html>
"@

                Write-Output $HtmlOutput
            }
            else
            {
                "Error: $($response.StatusCode)"
            }
        }
    }
}
