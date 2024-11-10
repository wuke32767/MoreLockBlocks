local glassLockBlockController = {}

glassLockBlockController.name = "MoreLockBlocks/GlassLockBlockController"
glassLockBlockController.depth = 0
glassLockBlockController.texture = "@Internal@/northern_lights"
glassLockBlockController.placements = {
    {
        name = "controller",
        data = {
            starColors = "ff7777,77ff77,7777ff,ff77ff,77ffff,ffff77",
            bgColor = "302040",
            lineColor = "ffffff",
            rayColor = "ffffff",
            wavy = false,
            vanillaEdgeBehavior = false,
            persistent = false
        }
    },
    {
        name = "controller_vanilla",
        data = {
            starColors = "7f9fba,9bd1cd,bacae3",
            bgColor = "0d2e89",
            lineColor = "ffffff",
            rayColor = "ffffff",
            wavy = true,
            vanillaEdgeBehavior = true,
            persistent = false
        }
    },
}

glassLockBlockController.fieldInformation = {
    bgColor = {
        fieldType = "color"
    },
    lineColor = {
        fieldType = "color"
    },
    rayColor = {
        fieldType = "color"
    },
    starColors = {
        fieldType = "list",
        elementOptions = {
            fieldType = "color",
        },
    }
}

return glassLockBlockController
