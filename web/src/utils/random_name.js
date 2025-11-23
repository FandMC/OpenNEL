/**
 * Generates a random int in [min, max]
 */
const nextInt = (min, max) => Math.floor(Math.random() * (max - min + 1)) + min

export const RandomName = {
    official: async () => {
        try {
            const url = appConfig.getRandomNameUrl()
            const res = await fetch(url)
            const data = await res.json()
            if (data && data.success && data.name) {
                return data.name
            }
        } catch {}
    },
    offline: (length = nextInt(10, 12)) => {
        const table = 'abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789_'
        let str = ''
        for (let i = 0; i < length; i++) {
            str += table[nextInt(0, table.length - 1)]
        }
        return str
    },
    gb2312: (length = nextInt(6, 8)) => {
        const gb2312Start = 0x4E00
        const gb2312End = 0x9FA5

        let str = ''
        for (let i = 0; i < length; i++) {
            const code = nextInt(gb2312Start, gb2312End)
            str += String.fromCharCode(code)
        }
        return str
    },
}
