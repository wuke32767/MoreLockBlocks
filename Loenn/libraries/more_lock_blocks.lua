local moreLockBlocks = {}

function moreLockBlocks.isempty(s)
    return s == nil or s == ""
end

function moreLockBlocks.dzhakeHelperKeySettingsValidator(settings)
    local trynumber = tonumber(settings)
    return settings == "" or settings == "*" or (trynumber ~= nil and not string.find(settings, ".", 1, true))
end

return moreLockBlocks
