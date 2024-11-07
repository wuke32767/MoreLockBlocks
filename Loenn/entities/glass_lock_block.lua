local drawableSprite = require("structs.drawable_sprite")
local drawableRectangle = require("structs.drawable_rectangle")
local utils = require("utils")

local glassLockBlock = {}

glassLockBlock.name = "MoreLockBlocks/GlassLockBlock"
glassLockBlock.depth = function(room, entity) return entity.behindFgTiles and -9995 or -10000 end
glassLockBlock.placements = {
    {
        name = "glassLockBlock",
        data = {
            unlock_sfx = "",
            stepMusicProgress = false,
            behindFgTiles = false,
        }
    }
}

function glassLockBlock.sprite(room, entity)
    local rectangle = drawableRectangle.fromRectangle(
        "bordered",
        entity.x, entity.y, 32, 32,
        { 1.0, 1.0, 1.0, 0.6 },
        { 1.0, 1.0, 1.0, 0.8 }
    )
    local lockSprite = drawableSprite.fromTexture("objects/MoreLockBlocks/glassLockBlock/lockdoor00", entity)
    lockSprite:addPosition(16, 16)

    return { rectangle, lockSprite }
end

function glassLockBlock.selection(room, entity)
    return utils.rectangle(entity.x, entity.y, 32, 32)
end

return glassLockBlock
