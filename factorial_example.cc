fact: i {
    if ! (i != 1) {
        return 1
    }

    return i * fact(i - 1)
}

main {
    while readi32() > 100 {
        print(fact(6))
    }
}