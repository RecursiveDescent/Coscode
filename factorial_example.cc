fact: i {
    if i - 0 {
        return 1
    }

    return i * fact(i - 1)
}

main {
    print(fact(6))
}