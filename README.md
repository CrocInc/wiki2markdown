WikiToMarkdown
=============

Confluence Wiki to Markdown (Github flavor) converter.

This tool accepts a xml backup from Confluence 3.x/4.x and converts all pages into Markdown.
Besides page the tool supports extracting attachments.

# Features
Currently it supports the following WIKI macros/markup:
* `{code}`
* `{noformat}`
* images: `!pic.png!`
* `{children}`
* TODO: `{toc}`
* paragraphes `\\`
* horizontal rules `----` 
* lists (ordered/unordered)
* headings
* inline code `{{term}}`
* links:
  * external
  * page-local (`[#section]`)
  * space-local (`[page1]`)
  * TODO: other-space (`[space1:page1]`) 
  * space-local with anchors (`[page2:section2]`)
* blocks `{note}/{warning}/{info}/{tip}`
* `{anchor}`
* GFM-tables - obviously wiki-tables cannot be seemlessly converted to Markdown as block markup (lists/code/etc) isn't supported inside tables

# CLI OPTIONS
The tool expect two mandatory arguments: path to directory or file with Confluence xml backup (extracted) and path to output directory.

Other options:
## -ext
Extension for output files

## -handlebars
Handlebars compliance - encode `{{` in output markup

## -frontmeta
A name of template of metadata for adding to every output file. See example below.

See Converter\TemplatePageProcessor.cs for details.

# EXAMPLE
```
..\bin\Debug\conf2md.exe myspace-193434-306.xml out -ext:.md -handlebars -frontmeta:front-meta.yml
```
where front-meta-yml is
```
---
title: {Title}
name: {Name}
area: Docs
section: {Section}
parent: {Parent}
order: {Position}
tags: {Tags}
root: {IsRoot}
---
```
It's additional metadata (YAML Front Matter for Assemble.io). It's completely optional.
