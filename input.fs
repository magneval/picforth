\ Input handling (object oriented)                      22oct00py

\ Copyright (C) 2000,2003 Free Software Foundation, Inc.

\ This file is part of Gforth.

\ Gforth is free software; you can redistribute it and/or
\ modify it under the terms of the GNU General Public License
\ as published by the Free Software Foundation; either version 2
\ of the License, or (at your option) any later version.

\ This program is distributed in the hope that it will be useful,
\ but WITHOUT ANY WARRANTY; without even the implied warranty of
\ MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
\ GNU General Public License for more details.

\ You should have received a copy of the GNU General Public License
\ along with this program; if not, write to the Free Software
\ Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111, USA.

\ input handling structure:

| : input-method ( m "name" -- m' )  Create dup , cell+
DOES> ( ... -- ... ) @ current-input @ @ + perform ;
| : input-var ( v size "name" -- v' )  Create  over , +
DOES> ( -- addr ) @ current-input @ + ;

0
input-method source ( -- addr u ) \ core-ext,file source
    \G Return address @i{addr} and length @i{u} of the current input
    \G buffer
input-method refill ( -- flag ) \ core-ext,block-ext,file-ext
    \G Attempt to fill the input buffer from the input source.  When
    \G the input source is the user input device, attempt to receive
    \G input into the terminal input device. If successful, make the
    \G result the input buffer, set @code{>IN} to 0 and return true;
    \G otherwise return false. When the input source is a block, add 1
    \G to the value of @code{BLK} to make the next block the input
    \G source and current input buffer, and set @code{>IN} to 0;
    \G return true if the new value of @code{BLK} is a valid block
    \G number, false otherwise. When the input source is a text file,
    \G attempt to read the next line from the file. If successful,
    \G make the result the current input buffer, set @code{>IN} to 0
    \G and return true; otherwise, return false.  A successful result
    \G includes receipt of a line containing 0 characters.
input-method source-id ( -- 0 | -1 | fileid ) \ core-ext,file source-i-d
    \G Return 0 (the input source is the user input device), -1 (the
    \G input source is a string being processed by @code{evaluate}) or
    \G a @i{fileid} (the input source is the file specified by
    \G @i{fileid}).
| input-method (save-input) ( -- x1 .. xn n ) \ gforth
| input-method (restore-input) ( x1 .. xn n -- ) \ gforth
drop

cell \ the first cell points to the method table
cell input-var >in \ core to-in
    \G @code{input-var} variable -- @i{a-addr} is the address of a
    \G cell containing the char offset from the start of the input
    \G buffer to the start of the parse area.
cell input-var #tib \ core-ext number-t-i-b
    \G @code{input-var} variable -- @i{a-addr} is the address of a
    \G cell containing the number of characters in the terminal input
    \G buffer. OBSOLESCENT: @code{source} superceeds the function of
    \G this word.
cell input-var max#tib \ gforth max-number-t-i-b
    \G @code{input-var} variable -- This cell contains the maximum
    \G size of the current tib.
cell input-var old-input \ gforth
    \G @code{input-var} variable -- This cell contains the pointer to
    \G the previous input buffer
cell input-var loadline \ gforth
    \G @code{input-var} variable -- This cell contains the line that's
    \G currently loaded from
1 [IF]
cell input-var loadfile \ gforth
    \G @code{input-var} variable -- This cell contains the file the
    \G input buffer is associated with (0 if none)
cell input-var blk \ block
    \G @code{input-var} variable -- This cell contains the current
    \G block number
cell input-var #fill-bytes \ gforth
    \G @code{input-var} variable -- number of bytes read via
    \G (read-line) by the last refill
2 cells input-var loadfilename \ gforth
    \G @code{input-var} variable -- addr u describes name of currently
    \G interpreted input (file name or somesuch)
[THEN]
0 input-var tib

Constant tib+

\ terminal input implementation

:noname ( in 1 -- ) 1 <> -12 and throw >in ! ;
                       \ restore-input
:noname ( -- in 1 ) >in @ 1 ;     \ save-input
' false                \ source-id
:noname ( -- flag ) [ 1 [IF] ] 
    stdin file-eof?  IF  false  EXIT  THEN [ [THEN] ]
    tib max#tib @ accept #tib !
    >in off true 1 loadline +! ;     \ refill
:noname ( -- addr u ) tib #tib @ ;   \ source

| Create terminal-input   A, A, A, A, A,
:noname ( -- addr u ) tib @ #tib @ ; \ source
| Create evaluate-input
    A,                  \ source
    ' false A,          \ refill
    ' true A,           \ source-id
    terminal-input 3 cells + @ A, \ terminal::restore-input
    terminal-input 4 cells + @ A, \ terminal::save-input

\ file input implementation

1 [IF]
: read-line ( c_addr u1 wfileid -- u2 flag wior ) (read-line) nip ;

:noname  ( in line# udpos 4 -- ) 4 <> -12 and throw
    loadfile @ reposition-file throw
    refill 0= -36 and throw \ should never throw
    loadline ! >in ! ; \ restore-input
:noname  ( -- in line# udpos 4 ) >in @ sourceline#
    loadfile @ file-position throw #fill-bytes @ 0 d-
    4 ;                \ save-input
:noname  ( -- file ) loadfile @ ;  \ source-id
:noname  ( -- flag )
    #tib off #fill-bytes off >in off
    BEGIN
	tib max#tib @ #tib @ /string
	loadfile @ (read-line) throw #fill-bytes +!
	swap #tib +!
	\ auto-expanding the tib
	dup #tib @ #fill-bytes @ = and WHILE
	    drop max#tib @ 2* expand-tib
    REPEAT
    1 loadline +! ;
                       \ refill
terminal-input @       \ source -> terminal-input::source

| Create file-input  A, A, A, A, A,
[THEN]

\ push-file, pop-file

: new-tib ( method n -- ) \ gforth
    \G Create a new entry of the tib stack, size @i{n}, method table
    \G @i{method}.
    dup >r tib+ + dup allocate throw tuck swap 0 fill
    current-input @ swap current-input ! old-input ! r> max#tib !
    current-input @ ! ;
: expand-tib ( n -- )
    dup tib+ + current-input @ swap resize throw current-input !
    max#tib ! tib max#tib @ #tib @ /string 0 fill ;
1 [IF]
: push-file  ( -- ) \ gforth
    \G Create a new file input buffer
    file-input def#tib new-tib ;
[THEN]
: pop-file ( throw-code -- throw-code ) \ gforth
    \G pop and free the current top input buffer
    dup IF
	source >in @ sourceline#
	[ 1 [IF] ] sourcefilename [ [THEN] ]
	>error
    THEN
    current-input @ old-input @ current-input ! free throw ;

\ save-input, restore-input

: save-input ( -- x1 .. xn n ) \ core-ext
    \G The @i{n} entries @i{xn - x1} describe the current state of the
    \G input source specification, in some platform-dependent way that can
    \G be used by @code{restore-input}.
    (save-input) current-input @ swap 1+ ;
: restore-input ( x1 .. xn n -- flag ) \ core-ext
    \G Attempt to restore the input source specification to the state
    \G described by the @i{n} entries @i{xn - x1}. @i{flag} is true if
    \G the restore fails.  In Gforth with the new input code, it fails
    \G only with a flag that can be used to throw again; it is also
    \G possible to save and restore between different active input
    \G streams. Note that closing the input streams must happen in the
    \G reverse order as they have been opened, but in between
    \G everything is allowed.
    current-input @ >r swap current-input ! 1- dup >r
    ['] (restore-input) catch
    dup IF  r> 0 ?DO  nip  LOOP  r> current-input !  EXIT  THEN
    rdrop rdrop ;

\ create terminal input block

: create-input ( -- )
    \G create a new terminal input
    terminal-input def#tib new-tib ;
    \ s" *the terminal*" loadfilename 2!

: execute-parsing ( ... addr u xt -- ... )
\G Make @i{addr u} the current input source, execute @i{xt @code{(
\G ... -- ... )}}, then restore the previous input source.
    >r evaluate-input cell new-tib
[ 1 [IF] ]
    s" *evaluated string*" loadfilename 2!
[ [THEN] ]
    -1 loadline ! #tib ! tib !
    r> catch pop-file throw ;

: evaluate ( ... addr u -- ... ) \ core,block
\G Save the current input source specification. Store @code{-1} in
\G @code{source-id} and @code{0} in @code{blk}. Set @code{>IN} to
\G @code{0} and make the string @i{c-addr u} the input source and
\G input buffer. Interpret. When the parse area is empty, restore the
\G input source specification.
    ['] interpret execute-parsing ;

\ clear tibstack

: clear-tibstack ( -- ) \ gforth
    \G clears the tibstack; if there is none, create the bottom entry:
    \G the terminal input buffer.
    current-input @ 0= IF  create-input  THEN
    BEGIN  old-input @  WHILE  0 pop-file drop  REPEAT ;

: query ( -- ) \ core-ext
    \G Make the user input device the input source. Receive input into
    \G the Terminal Input Buffer. Set @code{>IN} to zero. OBSOLESCENT:
    \G superceeded by @code{accept}.
    clear-tibstack  refill drop ;

\ load a file

1 [IF]
: read-loop ( i*x -- j*x ) \ gforth
    \G refill and interpret a file until EOF
    BEGIN  refill  WHILE  interpret  REPEAT ;

: execute-parsing-named-file ( i*x wfileid filename-addr filename-u xt -- j*x )
    >r push-file \ dup 2* cells included-files 2@ drop + 2@ type
    loadfilename 2!  loadfile !
    r> catch
    loadfile @ close-file swap 2dup or
    pop-file  drop throw throw ;

: execute-parsing-file ( i*x fileid xt -- j*x )
\G Make @i{fileid} the current input source, execute @i{xt @code{( i*x
\G -- j*x )}}, then restore the previous input source.
    s" *a file*" rot execute-parsing-named-file ;

: include-file ( i*x wfileid -- j*x )
    \G Interpret (process using the text interpreter) the contents of
    \G the file @var{wfileid}.
    ['] read-loop execute-parsing-file ;
[THEN]
