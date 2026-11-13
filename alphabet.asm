; ==========================================
; Alphabet Loop: Prints A to Z
; Uses INT 0x21 / AH=02h (char in DL)
; ==========================================
org 100h

start:
    mov cx, 26           ; Loop counter: 26 characters
    mov dl, 'A'          ; Start character

char_loop:
    mov ah, 2            ; DOS function 2: Display character
    int 0x21             ; Print char in DL
    inc dl               ; Advance character
    loop char_loop       ; Decrement CX, loop if CX != 0

    ; Print line feed
    mov ah, 2
    mov dl, 10
    int 0x21

    int 20               ; Terminate
